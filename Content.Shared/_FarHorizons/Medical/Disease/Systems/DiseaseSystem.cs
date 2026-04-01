using System.Linq;
using Content.Shared.Body.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Medical.Disease.Components;
using Content.Shared.Medical.Disease.Cures;
using Content.Shared.Medical.Disease.Prototypes;
using Content.Shared.Medical.Disease.Symptoms;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using Content.Shared.Dataset;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Medical.Disease.Systems;

/// <summary>
/// Server system that progresses diseases, triggers symptom behaviors, and handles spread/immunity.
/// </summary>
public sealed partial class SharedDiseaseSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDiseaseSymptomSystem _symptoms = default!;
    [Dependency] private readonly SharedDiseaseCureSystem _cure = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedInternalsSystem _internals = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private static readonly string _firstStrainName = "StrainFirstNames";
    private static readonly string _secondStrainName = "StrainSecondNames";

    /// <inheritdoc/>
    /// <summary>
    /// Processes carriers on their scheduled ticks.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiseaseCarrierComponent>();
        var now = _timing.CurTime;
        var carriersToProcess = new List<(EntityUid uid, DiseaseCarrierComponent carrier)>();

        while (query.MoveNext(out var uid, out var carrier))
        {
            if (carrier.NextTick > now)
                continue;

            carrier.NextTick = now + carrier.TickDelay;
            Dirty(uid, carrier);
            carriersToProcess.Add((uid, carrier));
        }

        foreach (var (uid, carrier) in carriersToProcess)
        {
            ProcessCarrier((uid, carrier));
        }
    }

    /// <summary>
    /// Advances disease stages and triggers symptom behaviors when eligible.
    /// Removes invalid diseases.
    /// </summary>
    private void ProcessCarrier(Entity<DiseaseCarrierComponent> ent)
    {
        // Attempts to decay the immunities of the carrier
        _cure.PostImmunityDecay(ent);

        if (ent.Comp.ActiveDiseases.Count == 0)
        {
            if (!string.IsNullOrEmpty(ent.Comp.DiseaseIcon))
            {
                ent.Comp.DiseaseIcon = string.Empty;
                Dirty(ent);
            }
            return;
        }

        var dirty = false;
        var toRemove = new ValueList<DiseaseData>();

        foreach (var (DiseaseData, stageData) in ent.Comp.ActiveDiseases.ToArray())
        {
            if (!_prototypes.TryIndex(DiseaseData.Id, out var disease))
            {
                toRemove.Add(DiseaseData);
                continue;
            }

            // Incubation: if still incubating, skip symptoms and spreading-level logic.
            if (ent.Comp.IncubatingUntil.TryGetValue(DiseaseData, out var until) && until > _timing.CurTime)
                continue;

            // Progression: scale advance chance strictly according to StageProb and time between ticks.
            var newStage = AdvanceStage(ent, DiseaseData, stageData);

            if (newStage != stageData)
            {
                UpdateBloodData(ent);
                ent.Comp.ActiveDiseases[DiseaseData] = stageData;
                dirty = true;
            }

            // Trigger configured stage effects.
            TriggerStage(ent, DiseaseData, stageData);

            // Attempt passive cure steps for this disease.
            _cure.TriggerCureSteps(ent, DiseaseData);
        }

        foreach (var id in toRemove)
        {
            ent.Comp.ActiveDiseases.Remove(id);
            dirty = true;
        }

        // Update HUD icon.
        UpdateIcon(ent);

        if (dirty)
            Dirty(ent);
    }

    private StageData AdvanceStage(Entity<DiseaseCarrierComponent> ent, DiseaseData disease, StageData currentStage)
    {
        if(!_prototypes.TryIndex(disease.Id, out var diseaseProto))
            return currentStage;
            
        var maxStage = Math.Max(0, diseaseProto.Stages.Count-1);
        if(currentStage.Stage == maxStage)
            return currentStage;
            
        if (currentStage.MinStageUntil > _timing.CurTime)
            return currentStage;

        // If max time exceeded, force stage change
        if (currentStage.MaxStageUntil < _timing.CurTime)
        {
            // Force advance change
            currentStage.MinStageUntil = _timing.CurTime + TimeSpan.FromSeconds(diseaseProto.Stages[currentStage.Stage].MinStageTime);
            currentStage.MaxStageUntil = _timing.CurTime + TimeSpan.FromSeconds(diseaseProto.Stages[currentStage.Stage].MaxStageTime);
            currentStage.Stage = Math.Min(currentStage.Stage + 1, maxStage);
            return currentStage; 
        }
        
        // Normal stage change.
        var perTickAdvance = Math.Clamp(disease.StageProb, 0f, 1f);
        var seed = SharedRandomExtensions.HashCodeCombine([(int)_timing.CurTick.Value, GetNetEntity(ent).Id, currentStage.MinStageUntil.Microseconds, currentStage.Stage]);
        var rand = new System.Random(seed);
        
        if (!rand.Prob(perTickAdvance))
            return currentStage;
            
        // Advance stage
        currentStage.MinStageUntil = _timing.CurTime + TimeSpan.FromSeconds(diseaseProto.Stages[currentStage.Stage].MinStageTime);
        currentStage.MaxStageUntil = _timing.CurTime + TimeSpan.FromSeconds(diseaseProto.Stages[currentStage.Stage].MaxStageTime);
        currentStage.Stage = Math.Min(currentStage.Stage + 1, maxStage);
        return currentStage; 
    }

    private void TriggerStage(Entity<DiseaseCarrierComponent> ent, DiseaseData disease, StageData stage)
    {
        if(!_prototypes.TryIndex(disease.Id, out var diseaseProto))
            return;

        var stageCfg = diseaseProto.Stages.FirstOrDefault(s => s.Stage == stage.Stage);
        if (stageCfg == null)
            return;

        // Uses popup
        for (var i = 0; i < stageCfg.Sensations.Count; i++)
        {
            var prob = 0.05f;
            // TODO: Replace with RandomPredicted once the engine PR is merged
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id, stage.MinStageUntil.Microseconds, stage.Stage, i);
            var rand = new System.Random(seed);
            if (!rand.Prob(prob))
                continue;

            _popup.PopupClient(Loc.GetString(stageCfg.Sensations[i]), ent.Owner);
        }

        // Symptoms are a list of detailed entries (symptom + optional probability override).
        for (var i = 0; i < stageCfg.Symptoms.Count; i++)
        {
            var entry = stageCfg.Symptoms[i];
            var symptomId = entry.Symptom;
            if (!_prototypes.TryIndex(symptomId, out var symptom))
                continue;

            // Skip if this symptom is currently suppressed by a symptom-level cure.
            if (ent.Comp.SuppressedSymptoms.TryGetValue(symptomId, out var value) && value > _timing.CurTime)
                continue;

            var prob = entry.Probability >= 0f ? entry.Probability : symptom.Probability;
            // TODO: Replace with RandomPredicted once the engine PR is merged
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id, stage.MinStageUntil.Microseconds, stage.Stage, i);
            var rand = new System.Random(seed);
            if (!rand.Prob(prob))
                continue;

            _symptoms.TriggerSymptom(ent, disease, symptom);
        }
    }

    private void UpdateIcon(Entity<DiseaseCarrierComponent> ent)
    {
        var selected = string.Empty;
        var bestPriority = int.MinValue;

        foreach (var (id, _) in ent.Comp.ActiveDiseases)
        {
            if (!_prototypes.TryIndex(id.Id, out var diseaseProto))
                continue;

            if (diseaseProto.IconDisease is not { } iconId)
                continue;

            if (!_prototypes.TryIndex(iconId, out var iconProto))
                continue;

            if (iconProto.PriorityDisease > bestPriority)
            {
                bestPriority = iconProto.PriorityDisease;
                selected = iconId;
            }
        }

        if (ent.Comp.DiseaseIcon != selected)
        {
            ent.Comp.DiseaseIcon = selected;
            Dirty(ent);
        }
    }

    /// <summary>
    /// Helper that finds an entity in a specific flagged slot, if present.
    /// </summary>
    private bool TryGetInventoryEntity(EntityUid target, SlotFlags flags, out EntityUid item)
    {
        var enumerator = _inventory.GetSlotEnumerator((target, CompOrNull<InventoryComponent>(target)), flags);
        if (enumerator.NextItem(out item))
            return true;

        item = default;
        return false;
    }

    /// <summary>
    /// Adjusts airborne infection chance for PPE/internals on the target.
    /// </summary>
    public float AdjustAirborneChanceForProtection(EntityUid target, float baseChance, DiseaseData disease)
    {
        var chance = baseChance;

        if (_internals.AreInternalsWorking(target))
            chance *= DiseaseEffectiveness.InternalsMultiplier;

        var permeability = MathF.Max(0f, disease.PermeabilityMod);
        foreach (var (slot, mult) in DiseaseEffectiveness.AirborneSlots)
        {
            if (slot == SlotFlags.MASK)
            {
                if (TryGetInventoryEntity(target, SlotFlags.MASK, out var maskUid))
                {
                    if (TryComp<MaskComponent>(maskUid, out var mask) && !mask.IsToggled)
                        chance *= MathF.Min(1f, mult * permeability);
                }
                continue;
            }

            if (TryGetInventoryEntity(target, slot, out _))
                chance *= MathF.Min(1f, mult * permeability);
        }

        return MathF.Max(0f, MathF.Min(1f, chance));
    }

    /// <summary>
    /// Adjusts contact infection chance for PPE on the target.
    /// </summary>
    public float AdjustContactChanceForProtection(EntityUid target, float baseChance, DiseaseData disease)
    {
        var chance = baseChance;
        var permeability = MathF.Max(0f, disease.PermeabilityMod);
        foreach (var (slot, mult) in DiseaseEffectiveness.ContactSlots)
        {
            if (TryGetInventoryEntity(target, slot, out _))
                chance *= MathF.Min(1f, mult * permeability);
        }

        return MathF.Max(0f, MathF.Min(1f, chance));
    }

    /// <summary>
    /// Validates if an entity can be infected with a particular disease (alive and prototype exists).
    /// </summary>
    public bool CanBeInfected(EntityUid uid, DiseaseData diseaseId)
    {
        if (!_prototypes.HasIndex(diseaseId.Id))
            return false;

        if (!TryComp<DiseaseCarrierComponent>(uid, out var carrier) || carrier.ActiveDiseases.Any(d => d.Key.Id == diseaseId.Id))
            return false;

        if(HasComp<PreventInfectionComponent>(uid))
            return false;

        if (_mobState.IsDead(uid))
            return false;

        return true;
    }

    /// <summary>
    /// Rolls probability, validates eligibility, then infects.
    /// </summary>
    public bool TryInfectWithChance(EntityUid uid, DiseaseData disease, StageData stage, float probability)
    {
        if (!CanBeInfected(uid, disease))
            return false;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine([(int)_timing.CurTick.Value, uid.GetHashCode(), 0]);
        var rand = new System.Random(seed);
        if (!rand.Prob(probability))
            return false;

        if (TryComp<DiseaseCarrierComponent>(uid, out var carrier) && carrier.Immunity.TryGetValue(disease, out var immunityStrength))
        {
            // Roll against immunity strength.
            // TODO: Replace with RandomPredicted once the engine PR is merged
            var seedImmunity = SharedRandomExtensions.HashCodeCombine([(int)_timing.CurTick.Value, uid.GetHashCode(), 1]);
            var randImmunity = new System.Random(seedImmunity);
            if (!randImmunity.Prob(immunityStrength))
                return false;
        }

        return Infect(uid, disease, stage);
    }

    /// <summary>
    /// Infects an entity if eligible, when it has a carrier component, and sets the initial stage.
    /// </summary>
    public bool Infect(EntityUid uid, DiseaseData disease, StageData stage)
    {
        if (!TryComp<DiseaseCarrierComponent>(uid, out var carrier))
            return false;

        // Only initialize stage and incubation when this disease is first added to the carrier.
        if (carrier.ActiveDiseases.TryAdd(disease, stage))
        {
            // Set initial stage.

            // Schedule incubation window if configured; during incubation symptoms/spread are suppressed.
            if (disease.IncubationSeconds > 0)
                carrier.IncubatingUntil[disease] = _timing.CurTime + TimeSpan.FromSeconds(disease.IncubationSeconds);
        }

        UpdateBloodData((uid,carrier));
        carrier.NextTick = _timing.CurTime + carrier.TickDelay;
        Dirty(uid, carrier);
        return true;
    }

    public DiseaseData? CreateDisease(string diseaseId)
    {
        if (!_prototypes.TryIndex<DiseasePrototype>(diseaseId, out var proto))
            return null;
        
        var disease = new DiseaseData
        {
            Id = diseaseId,
            Name = proto.Name,
            Description = proto.Description,
            StrainName = GenerateStrainName(),
            SpreadPath = proto.SpreadPath,
            StageProb = proto.StageProb,
            PostCureImmunity = proto.PostCureImmunity,
            IncubationSeconds = proto.IncubationSeconds,
            ContactInfect = proto.ContactInfect,
            ContactDeposit = proto.ContactDeposit,
            AirborneInfect = proto.AirborneInfect,
            AirborneRange = proto.AirborneRange
        };
        return disease;
    }

    public StageData? CreateStage(string diseaseId, int startStage=0)
    {
        if (!_prototypes.TryIndex<DiseasePrototype>(diseaseId, out var proto))
            return null;
        
        var stage = new StageData
        {
            Stage = startStage,
            MinStageUntil = _timing.CurTime + TimeSpan.FromSeconds(proto.Stages[startStage].MinStageTime),
            MaxStageUntil = _timing.CurTime + TimeSpan.FromSeconds(proto.Stages[startStage].MaxStageTime)
        };
        return stage;
    }

    private string GenerateStrainName()
        => $"{_random.Pick(_prototypes.Index<LocalizedDatasetPrototype>(_firstStrainName))}-{_random.NextByte(99)} {_random.Pick(_prototypes.Index<LocalizedDatasetPrototype>(_secondStrainName))}";

    public void UpdateBloodData(Entity<DiseaseCarrierComponent> ent)
    {
        if (!TryComp<BloodstreamComponent>(ent.Owner, out var bloodstream)
            || !_solution.ResolveSolution(ent.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution)) return;
        var bloodReferenceData = bloodstream.BloodReferenceSolution.FirstOrDefault().Reagent.Data;

        if (bloodReferenceData == null) return;

        var bloodDiseaseData = bloodReferenceData.OfType<DiseaseReagentData>().FirstOrDefault();

        if (bloodDiseaseData == null)
        {
            bloodDiseaseData = new DiseaseReagentData
            {
                ActiveDiseases = new Dictionary<DiseaseData, StageData>(ent.Comp.ActiveDiseases),
                Immunity = new Dictionary<DiseaseData, float>(ent.Comp.Immunity)
            };

            bloodReferenceData.Add(bloodDiseaseData);
        }
        else
        {
            bloodDiseaseData.ActiveDiseases.Clear();
            bloodDiseaseData.Immunity.Clear();

            foreach (var (disease, stage) in ent.Comp.ActiveDiseases)
                bloodDiseaseData.ActiveDiseases[disease] = stage;

            foreach (var (disease, value) in ent.Comp.Immunity)
                bloodDiseaseData.Immunity[disease] = value;
        }
        bloodSolution.SetReagentData(bloodReferenceData);
    }
}
