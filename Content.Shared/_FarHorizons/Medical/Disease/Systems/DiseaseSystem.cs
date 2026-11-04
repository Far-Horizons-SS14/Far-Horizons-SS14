using System.Linq;
using Content.Shared.Body.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared._FarHorizons.Medical.Disease.Cures;
using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Content.Shared._FarHorizons.Medical.Disease.Symptoms;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Dataset;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Network;
using Content.Shared.Metabolism;
using Content.Shared.StatusEffectNew;
using Content.Shared.Zombies;

namespace Content.Shared._FarHorizons.Medical.Disease.Systems;

/// <summary>
/// Server system that progresses diseases, triggers symptom behaviors, and handles spread/immunity.
/// </summary>
public sealed partial class SharedDiseaseSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedDiseaseSymptomSystem _symptoms = default!;
    [Dependency] private SharedDiseaseCureSystem _cure = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedInternalsSystem _internals = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private StatusEffectsSystem _effect = default!;

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
        var stages = _prototypes.Index(disease.Vector).Timers;
        var maxStage = stages.Count;
        
        if(currentStage.Stage == maxStage)
            return currentStage;

        if (currentStage.AdvanceStageAt > _timing.CurTime)
            return currentStage;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id, currentStage.AdvanceStageAt.Microseconds, stages[currentStage.Stage]);
        var rand = new System.Random(seed);
        var mod = NextFloat(rand, disease.DiseaseTimerThresholds.X, disease.DiseaseTimerThresholds.Y);
        currentStage.AdvanceStageAt = _timing.CurTime + TimeSpan.FromSeconds(stages[currentStage.Stage] * mod);
        currentStage.Stage = Math.Min(currentStage.Stage + 1, maxStage);
        return currentStage; 
    }

    private void TriggerStage(Entity<DiseaseCarrierComponent> ent, DiseaseData disease, StageData stage)
    {
        for (var i = 0; i < disease.Symptoms.Count; i++)
        {
            var symptomId = disease.Symptoms[i].Symptom;
            if (!_prototypes.TryIndex(symptomId, out var symptom))
                continue;

            if (ent.Comp.SuppressedSymptoms.TryGetValue(symptomId, out var value) && value > _timing.CurTime)
                continue;
            
            _symptoms.TriggerSymptom(ent, disease, stage, symptom);
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
        var protection = 0f;

        var permeability = MathF.Max(0f, disease.PermeabilityMod);
        foreach (var (slot, mult) in DiseaseEffectiveness.AirborneSlots)
        {
            if (slot == SlotFlags.MASK)
            {
                if (TryGetInventoryEntity(target, SlotFlags.MASK, out var maskUid))
                {
                    if (TryComp<MaskComponent>(maskUid, out var mask) && !mask.IsToggled)
                        protection += (1f - mult) * permeability;
                }
                continue;
            }
            if (TryGetInventoryEntity(target, slot, out _))
                protection += (1f - mult) * permeability;
        }

        var zombificationResistanceEv = new ZombificationResistanceQueryEvent(DiseaseEffectiveness.InfectionProtectionSlots);
        RaiseLocalEvent(target, zombificationResistanceEv);
        protection += 1f * (1f - zombificationResistanceEv.TotalCoefficient);

        if (_internals.AreInternalsWorking(target))
            protection = 1f;

        return MathF.Max(0f, chance * (1f - MathF.Min(1f, protection)));
    }

    /// <summary>
    /// Adjusts contact infection chance for PPE on the target.
    /// </summary>
    public float AdjustContactChanceForProtection(EntityUid target, float baseChance, DiseaseData disease)
    {
        var protection = 0f;
        var permeability = MathF.Max(0f, disease.PermeabilityMod);
        foreach (var (slot, mult) in DiseaseEffectiveness.ContactSlots)
        {
            if (TryGetInventoryEntity(target, slot, out _))
                protection += (1f - mult) * permeability;
        }

        var zombificationResistanceEv = new ZombificationResistanceQueryEvent(DiseaseEffectiveness.InfectionProtectionSlots);
        RaiseLocalEvent(target, zombificationResistanceEv);
        protection += 1f * (1f - zombificationResistanceEv.TotalCoefficient);
        
        return MathF.Max(0f, baseChance * (1f - MathF.Min(1f, protection)));
    }

    /// <summary>
    /// Validates if an entity can be infected with a particular disease (alive and prototype exists).
    /// </summary>
    public bool CanBeInfected(EntityUid uid, DiseaseData diseaseId)
    {
        if (!_prototypes.HasIndex(diseaseId.Id) 
        || !TryComp<DiseaseCarrierComponent>(uid, out var carrier) || carrier.ActiveDiseases.Any(d => d.Key.Id == diseaseId.Id)
        || _effect.HasStatusEffect(uid, new EntProtoId("StatusEffectDiseaseImmunity"))
        || HasComp<PreventInfectionComponent>(uid)
        || _mobState.IsDead(uid))
            return false;

        if(diseaseId.MetabolizerTypes != null 
            && TryComp<MetabolizerComponent>(uid, out var metabolizer)
            && metabolizer.MetabolizerTypes != null
            && !metabolizer.MetabolizerTypes.Overlaps(diseaseId.MetabolizerTypes))
            return false;

        return true;
    }

    /// <summary>
    /// Rolls probability, validates eligibility, then infects.
    /// </summary>
    public bool TryInfectWithChance(EntityUid uid, DiseaseData disease, StageData stage, float probability)
    {
        if(probability < 0.01)
            return false;

        if (!CanBeInfected(uid, disease))
            return false;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine([(int)_timing.CurTick.Value, uid.GetHashCode(), stage.AdvanceStageAt.GetHashCode()]);
        var rand = new System.Random(seed);

        if (rand.NextDouble() > probability)
            return false;

        if (TryComp<DiseaseCarrierComponent>(uid, out var carrier) && carrier.Immunity.TryGetValue(disease, out var immunityStrength))
        {
            // Roll against immunity strength.
            // TODO: Replace with RandomPredicted once the engine PR is merged
            var seedImmunity = SharedRandomExtensions.HashCodeCombine([(int)_timing.CurTick.Value, uid.GetHashCode(), stage.AdvanceStageAt.GetHashCode()]);
            var randImmunity = new System.Random(seedImmunity);
            if (randImmunity.NextDouble() > 1f-immunityStrength)
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

        if(!CanBeInfected(uid, disease))
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

    public DiseaseData? GenerateDisease(string diseaseId)
    {
        if (!_prototypes.TryIndex<DiseasePrototype>(diseaseId, out var proto))
            return null;
        
        var disease = new DiseaseData
        {
            Id = diseaseId,
            Name = proto.Name,
            Description = proto.Description,
            StrainName = proto.StrainName,
            StrainId = proto.StrainId,
            Vector = proto.Vector,
            Symptoms = proto.Symptoms,
            CureSteps = proto.CureSteps,
            MetabolizerTypes = proto.MetabolizerTypes,
            Stealth = proto.Stealth,
            SpreadPath = proto.SpreadPath,
            DiseaseTimerThresholds = proto.DiseaseTimerThresholds,
            PostCureImmunity = proto.PostCureImmunity,
            IncubationSeconds = proto.IncubationSeconds,       
            PermeabilityMod = proto.PermeabilityMod,
            ContactInfect = proto.ContactInfect,
            ContactDeposit = proto.ContactDeposit,
            AirborneInfect = proto.AirborneInfect,
            AirborneRange = proto.AirborneRange,
            IconDisease = proto.IconDisease
        };

        disease.Stats = GetTotalDiseaseStats(disease);

        return disease;
    }

    public DiseaseData? UpdateDisease(DiseaseData disease)
    {
        //Basic Info
        disease.Name = "Unknown Disease";
        disease.Description = "Unknown Disease";

        var strainInfo = GenerateStrainName();
        disease.StrainName = strainInfo.Item2;
        disease.StrainId = strainInfo.Item1;
        disease.Stats = GetTotalDiseaseStats(disease);

        //Stealth 

        disease.Stealth = disease.Stats.Stealth switch
        {
            > 5 => DiseaseStealthFlags.Hidden | DiseaseStealthFlags.VeryHidden,
            > 3 => DiseaseStealthFlags.Hidden,
            _   => DiseaseStealthFlags.None,
        };

        // Resistance
        
        disease.PostCureImmunity  = Math.Max(0f, disease.PostCureImmunity - (disease.Stats.Resistance / 20f));
        var cures = _prototypes.EnumeratePrototypes<CurePrototype>().Where(p => p.Tier.Equals(Math.Clamp(disease.Stats.Resistance, 0, 10)));
        var maxCures = 2;
        List<CureStep> selectedCures = new();

        var possibleCures = cures.Select(c => c.CureStep).OfType<CureStep>().ToList();

        for (int i = 0; i < maxCures && possibleCures.Count > 0; i++)
        {
            var seed = SharedRandomExtensions.HashCodeCombine(
                (int) _timing.CurTick.Value,
                disease.Stats.Resistance,
                disease.Stats.Stealth,
                disease.Stats.Speed,
                disease.Stats.Transmittable,
                i);

            var rand = new System.Random(seed);
            var idx = rand.Next(possibleCures.Count);

            selectedCures.Add(possibleCures[idx]);
            possibleCures.RemoveAt(idx);
        }

        disease.CureSteps = new List<CureStep>
        {
            new CureConditions {Conditions = selectedCures},
            new CureWait { RequiredTicks = 900 },
            new CureBedrest { BedrestChance = 0.0033f, SleepMultiplier = 5f}
        };

        // Speed

        var reduction = disease.Stats.Speed / 20f;
        disease.DiseaseTimerThresholds -= new System.Numerics.Vector2(reduction, reduction);

        // Transmittable

        disease.SpreadPath = disease.Stats.Transmittable switch
        {
            > 99 => DiseaseSpreadPath.Special,
            > 5  => DiseaseSpreadPath.Airborne,
            > 2  => DiseaseSpreadPath.Contact,
            > 0  => DiseaseSpreadPath.Blood,
            _    => DiseaseSpreadPath.NonContagious,
        };
        
        disease.ContactInfect = Math.Clamp(disease.ContactInfect * (1 + (disease.Stats.Transmittable / 20f)), -20f, 99);
        disease.ContactDeposit = Math.Clamp(disease.ContactDeposit * (1 + (disease.Stats.Transmittable / 20f)), -20f, 99);
        disease.AirborneInfect = Math.Clamp(disease.AirborneInfect * (1 + (disease.Stats.Transmittable / 20f)), -20f, 99);
        disease.AirborneRange = Math.Clamp(disease.AirborneRange * (1 + (disease.Stats.Transmittable / 20f)), -20f, 99);

        return disease;
    }

    public StageData? CreateStage(DiseaseData disease, int startStage=0)
    {   
        var stages = _prototypes.Index(disease.Vector).Timers;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, startStage);
        var rand = new System.Random(seed);

        var timerModifier = NextFloat(rand, disease.DiseaseTimerThresholds.X, disease.DiseaseTimerThresholds.Y);
        var stage = new StageData
        {
            Stage = startStage,
            AdvanceStageAt = _timing.CurTime + TimeSpan.FromSeconds(stages[startStage] * timerModifier)
        };
        return stage;
    }

    private (string, string) GenerateStrainName()
    {
        const int MaxValue = 99;
        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, MaxValue);
        var rand = new System.Random(seed);

        var firstOptions = _prototypes.Index<LocalizedDatasetPrototype>(_firstStrainName).Values;
        var secondOptions = _prototypes.Index<LocalizedDatasetPrototype>(_secondStrainName).Values;

        var first = firstOptions[rand.Next(firstOptions.Count)];
        var second = secondOptions[rand.Next(secondOptions.Count)];
        var number = rand.Next(MaxValue);

        return ($"{first}{number}{second}", $"{first}-{number} {second}");
    }

    private DiseaseStats GetTotalDiseaseStats(DiseaseData disease)
    {
        var stats = _prototypes.Index(disease.Vector).Stats;

        foreach(var symptomID in disease.Symptoms)
        {
            if(!_prototypes.TryIndex(symptomID.Symptom, out var symptom))    
                continue;
            
            stats.Resistance += symptom.Stats.Resistance;
            stats.Stealth += symptom.Stats.Stealth;
            stats.Speed += symptom.Stats.Speed;
            stats.Transmittable += symptom.Stats.Transmittable;
        }
        stats.Resistance = Math.Max(stats.Resistance, 0);
        stats.Stealth = Math.Max(stats.Stealth, 0);
        stats.Speed = Math.Max(stats.Speed, 0);
        stats.Transmittable = Math.Max(stats.Transmittable, 0);

        return stats;
    }

    public void UpdateBloodData(Entity<DiseaseCarrierComponent> ent)
    {
        if(_net.IsClient) return;

        if (!TryComp<BloodstreamComponent>(ent.Owner, out var bloodstream)
            || !_solution.ResolveSolution(ent.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution)) return;

        var bloodData = _bloodstream.GetEntityBloodData((ent.Owner, bloodstream));
        DiseaseReagentData? diseaseData = null;
        foreach (var d in bloodData)
        {
            if (d is DiseaseReagentData dd)
            {
                diseaseData = dd;
                break;
            }
        }
        if(diseaseData == null)
        {
            diseaseData = new DiseaseReagentData();
            bloodData.Add(diseaseData);
        }
    
        diseaseData.ActiveDiseases = new Dictionary<DiseaseData, StageData>(ent.Comp.ActiveDiseases);
        diseaseData.Immunity = new Dictionary<DiseaseData, float>(ent.Comp.Immunity);
        bloodstream.BloodReferenceSolution.SetReagentData(bloodData);

        var refPrototypes = bloodstream.BloodReferenceSolution.Contents
            .Select(x => x.Reagent.Prototype)
            .ToHashSet();

        for (var i = 0; i < bloodSolution.Contents.Count; i++)
        {
            var old = bloodSolution.Contents[i];
            if (refPrototypes.Contains(old.Reagent.Prototype))
                bloodSolution.Contents[i] = new ReagentQuantity(new ReagentId(old.Reagent.Prototype, bloodData), old.Quantity);
        }

        Dirty(ent);
        Dirty(ent.Owner, bloodstream);
    }

    private static float NextFloat(System.Random rand, float min, float max)
        => min + (rand.NextSingle() * (max - min));
}
