using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Robust.Shared.Prototypes;
using Content.Shared.Popups;
using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared._FarHorizons.Medical.Disease.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Disease.Systems;

/// <summary>
/// Handles using a DiseaseSample on the DiseaseDiagnoser to print a report.
/// TODO: There should be more features. The current implementation provides a minimal implementation for diagnostics.
/// </summary>
public sealed partial class DiseaseDiagnoserSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PaperSystem _paper = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseDiagnoserComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<DiseaseDiagnoserComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<DiseaseSampleComponent>(args.Used, out var sample))
            return;

        // Reject if no sample material present.
        if (!sample.HasSample)
        {
            _popup.PopupPredicted(Loc.GetString("diagnoser-disease-empty-swab-popup"), ent.Owner, args.User);
            args.Handled = true;
            return;
        }

        args.Handled = true;

        // Build report text.
        var content = BuildReportContent(sample);

        // Spawn paper and set content.
        var paperUid = PredictedSpawnAtPosition(ent.Comp.PaperPrototype, Transform(ent.Owner).Coordinates);
        if (TryComp<PaperComponent>(paperUid, out var paperComp))
        {
            _paper.SetContent((paperUid, paperComp), content);
        }

        // Clear diagnoser state if any and clear sample to avoid reusing stale data.
        sample.DiseasesData.Clear();
        sample.SubjectName = null;
        sample.SubjectDNA = null;
        sample.HasSample = false;
        Dirty(args.Used, sample);

        _popup.PopupPredicted(Loc.GetString("diagnoser-disease-printed-popup"), ent.Owner, args.User);
    }

    private string BuildReportContent(DiseaseSampleComponent sample)
    {
        var headerParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(sample.SubjectName))
            headerParts.Add(Loc.GetString("diagnoser-disease-report-subject", ("name", sample.SubjectName!)));

        if (!string.IsNullOrWhiteSpace(sample.SubjectDNA))
            headerParts.Add(Loc.GetString("diagnoser-disease-report-subject-dna", ("dna", sample.SubjectDNA!)));

        var header = string.Join("\n", headerParts);

        if (sample.DiseasesData.Count == 0)
        {
            var healthy = Loc.GetString("diagnoser-disease-report-none");
            return string.IsNullOrEmpty(header) ? healthy : $"{header}\n{healthy}";
        }

        var lines = new List<string>();
        foreach (var (diseaseData, stageData) in sample.DiseasesData)
        {
            if (!_prototypes.TryIndex(diseaseData.Id, out DiseasePrototype? diseaseProto))
                continue;
            
            var displayName = Loc.GetString(diseaseData.Name[0].ToString().ToUpper() + diseaseData.Name[1..]);
            var stage = stageData.Stage;

            var showStage = true;

            lines.Add(Loc.GetString("diagnoser-disease-report-name",("name", displayName)));
            lines.Add(Loc.GetString("diagnoser-disease-report-stage",("stage", showStage ? stage+1 : "Unknown")));
            lines.Add(Loc.GetString("diagnoser-disease-report-variant",("name", diseaseData.StrainName)));
            lines.Add(Loc.GetString("diagnoser-disease-report-spreadpath",("spreadpath", diseaseData.SpreadPath)));
            lines.Add(Loc.GetString("diagnoser-disease-report-desc"));
            lines.Add(Loc.GetString(diseaseData.Description));

            // Symptoms block.
            lines.Add(Loc.GetString("diagnoser-disease-symptoms-header"));
            if (diseaseData.Symptoms.Count == 0)
            {
                lines.Add("- " + Loc.GetString("diagnoser-disease-symptoms-none"));
            }
            else
            {
                foreach (var symptomEntry in diseaseData.Symptoms)
                {
                    var symptomId = symptomEntry.Symptom;
                    if (_prototypes.TryIndex(symptomId, out var symProto))
                    {
                        var symName = Loc.GetString(symProto.Name);
                        lines.Add("- " + symName);
                    }
                }
            }

            // Cures block.
            var cureSteps = diseaseData.CureSteps;
            var showTreatment = true;
            if (cureSteps.Count == 0)
            {
                lines.Add(Loc.GetString("diagnoser-no-cures"));
            }
            else if(!showTreatment)
            {
                lines.Add(Loc.GetString("diagnoser-cure-unknown"));
            }
            else
            {
                lines.Add(Loc.GetString("diagnoser-cure-has"));
                for (int i = 0; i < cureSteps.Count; i++)
                {
                    var step = cureSteps[i];
                    lines.Add("Option " + $"{i+1}:");
                    var stepLines = step.BuildDiagnoserLines(_prototypes).ToList();

                    foreach (var stepLine in stepLines)
                    {
                        var finalLine = stepLine;
                        var chance = Math.Clamp(step.CureChance, 0f, 1f);
                        if (chance is > 0f and < 1f)
                        {
                            var percent = MathF.Round(chance * 100f);
                            finalLine += $" ({percent}%)";
                        }

                        if (step.LowerStage)
                            lines.Add("- " + finalLine + ". " + Loc.GetString("diagnoser-cure-lower-stage"));
                        else
                            lines.Add("- " + finalLine + ". " + Loc.GetString("diagnoser-cure-lower-disease"));
                    }
                }
            }

            lines.Add("\n");
        }

        var body = string.Join("\n", lines);
        return string.IsNullOrEmpty(header) ? body : $"{header}\n{body}";
    }
}
