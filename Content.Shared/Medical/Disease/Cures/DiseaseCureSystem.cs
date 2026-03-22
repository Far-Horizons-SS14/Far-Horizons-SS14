using System.Linq;
using Content.Shared.Medical.Disease.Components;
using Content.Shared.Medical.Disease.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Disease.Cures;

public sealed partial class SharedDiseaseCureSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    /// <summary>
    /// Executes a configured cure step via its polymorphic OnCure.
    /// </summary>
    private bool ExecuteCureStep(Entity<DiseaseCarrierComponent> ent, CureStep step, DiseasePrototype disease)
    {
        var deps = _entitySystemManager.DependencyCollection;
        deps.InjectDependencies(step);
        return step.OnCure(ent.Owner, disease);
    }

    /// <summary>
    /// Attempts to apply cure steps for a disease on the provided carrier.
    /// </summary>
    public void TriggerCureSteps(Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease)
    {
        if (!ent.Comp.ActiveDiseases.TryGetValue(disease.ID, out var stageNum))
            return;

        var stageCfg = disease.Stages.FirstOrDefault(s => s.Stage == stageNum);
        if (stageCfg == null)
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine([(int)_timing.CurTick.Value, GetNetEntity(ent).Id]);
        var rand = new System.Random(seed);

        // Disease-level cures.
        var applicable = stageCfg.CureSteps.Count > 0 ? stageCfg.CureSteps : disease.CureSteps;
        foreach (var step in applicable)
        {
            // Calculates the probability of treatment at each tick.
            if (!rand.Prob(Math.Clamp(step.CureChance, 0f, 1f)))
                continue;

            if (!ExecuteCureStep(ent, step, disease))
                continue;

            if (step.LowerStage)
                ApplyCureDiseaseStage(ent, disease);
            else
                ApplyCureDisease(ent, disease);
        }

        // Symptom-level cures.
        foreach (var entry in stageCfg.Symptoms)
        {
            var symptomId = entry.Symptom;
            if (!_prototypes.TryIndex(symptomId, out var symptomProto))
                continue;

            // If symptom is currently suppressed (recently treated).
            if (ent.Comp.SuppressedSymptoms.TryGetValue(symptomId, out var until) && until > _timing.CurTime)
                continue;

            if (symptomProto.CureSteps.Count == 0)
                continue;

            foreach (var step in symptomProto.CureSteps)
            {
                if (!rand.Prob(Math.Clamp(step.CureChance, 0f, 1f)))
                    continue;

                if (ExecuteCureStep(ent, step, disease))
                    ApplyCureSymptom(ent, symptomId);
            }
        }
    }

    /// <summary>
    /// Removes the disease, applies post-cure immunity.
    /// </summary>
    public void ApplyCureDisease(Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease)
    {
        if (!ent.Comp.ActiveDiseases.ContainsKey(disease.ID))
            return;

        ent.Comp.ActiveDiseases.Remove(disease.ID);
        ApplyPostCureImmunity(ent.Comp, disease);

        _popup.PopupPredicted(Loc.GetString("disease-cured"), ent, ent.Owner);
    }

    /// <summary>
    /// Lowers the disease stage by 1.
    /// </summary>
    public void ApplyCureDiseaseStage(Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease)
    {
        if (!ent.Comp.ActiveDiseases.TryGetValue(disease.ID, out var stage) || stage <= 1)
            return;

        ent.Comp.ActiveDiseases[disease.ID] = stage - 1;
    }

    /// <summary>
    /// Suppresses the given symptom for its configured duration and notifies hooks.
    /// </summary>
    public void ApplyCureSymptom(Entity<DiseaseCarrierComponent> ent, string symptomId)
    {
        if (!_prototypes.TryIndex(symptomId, out DiseaseSymptomPrototype? symptomProto))
            return;

        var duration = symptomProto.CureDuration;
        if (duration <= 0f)
            return;

        ent.Comp.SuppressedSymptoms[symptomId] = _timing.CurTime + TimeSpan.FromSeconds(duration);

        _popup.PopupPredicted(Loc.GetString("disease-cured-symptom"), ent, ent.Owner);
    }

    /// <summary>
    /// Writes or raises the immunity strength for the cured disease on the carrier.
    /// </summary>
    private static void ApplyPostCureImmunity(DiseaseCarrierComponent comp, DiseasePrototype disease)
    {
        var strength = disease.PostCureImmunity;

        if (comp.Immunity.TryGetValue(disease.ID, out var existing))
            comp.Immunity[disease.ID] = MathF.Max(existing, strength);
        else
            comp.Immunity[disease.ID] = strength;
    }

    /// <summary>
    /// Runtime per-step state stored in the system.
    /// </summary>
    internal sealed class CureState
    {
        public float Ticker;
    }

    private readonly Dictionary<(EntityUid, string, CureStep), CureState> _cureStates = [];

    internal CureState GetState(EntityUid uid, string diseaseId, CureStep step)
    {
        var key = (uid, diseaseId, step);
        if (!_cureStates.TryGetValue(key, out var state))
        {
            state = new CureState();
            _cureStates[key] = state;
        }
        return state;
    }
}
