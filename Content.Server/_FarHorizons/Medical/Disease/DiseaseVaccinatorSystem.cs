using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Lathe;
using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Content.Shared._FarHorizons.Medical.Disease.UI;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Medical.Disease.Systems;

public sealed class DiseaseVaccinatorSystem : EntitySystem
{
    [Dependency] private readonly LatheSystem _lathe = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    private readonly static string _vaccine = "Vaccine";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DiseaseVaccinatorComponent, CreateVaccineMessage>(OnCreateVaccineMessage);
        SubscribeLocalEvent<DiseaseVaccinatorComponent, LatheProductFinishedEvent>(OnProductFinished);
    }

    private void OnCreateVaccineMessage(Entity<DiseaseVaccinatorComponent> ent, ref CreateVaccineMessage args)
    {
        if (_proto.TryIndex(_vaccine, out LatheRecipePrototype? recipe))
        {
            if (_lathe.TryAddToQueue(ent.Owner, recipe, 1))
            {
                _adminLogger.Add(LogType.Action,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):player} queued {1} {_lathe.GetRecipeName(recipe)} at {ToPrettyString(ent.Owner):lathe}");
            }
        }
        _lathe.TryStartProducing(ent.Owner);
    }

    private void OnProductFinished(Entity<DiseaseVaccinatorComponent> ent, ref LatheProductFinishedEvent args)
    {
        if(!_container.TryGetContainer(ent.Owner, "sample", out var sampleContainer)) return;
        var vial = sampleContainer.ContainedEntities.FirstOrNull();
        if (vial == null) return;
        if(!_solutionContainer.TryGetSolution(vial.Value, "drink", out var solution)) return;

        Dictionary<string, (DiseaseData disease, float value)> immunityByName = [];
        var immunities = solution.Value.Comp.Solution;
        var name = "";

        foreach(var immunity in immunities)
        {
            if(immunity.Reagent.Data == null)
                continue;
            var diseaseData = immunity.Reagent.Data.OfType<DiseaseReagentData>().FirstOrDefault();
            if (diseaseData == null)
                continue;
                
            foreach (var (disease, value) in diseaseData.Immunity)
            {
                string diseaseKey = disease.Name; 
                
                if (!immunityByName.TryGetValue(diseaseKey, out var existing) || value > existing.value)
                {
                    immunityByName[diseaseKey] = (disease, value);
                    name += $"{Loc.GetString(disease.Name)} {Math.Round(value*100)}% ";
                }
            }
        }
        name += "Vaccine";

        Dictionary<DiseaseData, float> Immunity = immunityByName.Values.ToDictionary(x => x.disease, x => x.value);

        Entity<SolutionComponent>? penSolRef = null;

        if (!_solutionContainer.ResolveSolution(args.Item, "pen", ref penSolRef, out var penSol)) return;

        _metadata.SetEntityName(args.Item, name);
        var reagentData = new List<ReagentData>();
        var DiseaseData = new DiseaseReagentData { Immunity = Immunity }; 
        reagentData.Add(DiseaseData);
        penSol.SetReagentData(reagentData);
    }
}
