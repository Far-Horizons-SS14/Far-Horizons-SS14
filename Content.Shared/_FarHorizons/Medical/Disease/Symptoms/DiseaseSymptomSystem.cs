using Content.Shared.Mobs.Systems;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Disease.Symptoms;

/// <summary>
/// Encapsulates symptom-side effects and secondary spread mechanics for diseases.
/// </summary>
public sealed partial class SharedDiseaseSymptomSystem : EntitySystem
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DiseaseAirborneSystem _airborneDisease = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <summary>
    /// Executes the side-effects for a triggered symptom on a carrier.
    /// </summary>
    public void TriggerSymptom(Entity<DiseaseCarrierComponent> ent, DiseaseData disease, DiseaseSymptomPrototype symptom)
    {
        // Skip this symptom when the carrier is dead.
        if (symptom.OnlyAlive && _mobState.IsDead(ent.Owner))
            return;

        var deps = _entitySystemManager.DependencyCollection;

        // Local helper to execute a single symptom behavior with dependencies injected.
        void RunSingleBehavior(SymptomBehavior behavior)
        {
            deps.InjectDependencies(behavior);
            behavior.OnSymptom(ent.Owner, disease);
        }

        if (symptom.SingleBehavior && symptom.Behaviors.Count > 0)
        {
            // Run exactly one random behavior.
            // TODO: Replace with RandomPredicted once the engine PR is merged
            var seed = SharedRandomExtensions.HashCodeCombine([(int)GetNetEntity(ent).Id, 0, 0, symptom.Behaviors.Count]);
            var rand = new System.Random(seed);
            var behavior = symptom.Behaviors[rand.Next(0, symptom.Behaviors.Count)];
            RunSingleBehavior(behavior);
        }
        else
        {
            // Run all behavior.
            foreach (var behavior in symptom.Behaviors)
            {
                RunSingleBehavior(behavior);
            }
        }

        ApplyAirborneBurst(symptom, ent, disease);
    }

    /// <summary>
    /// Applies a single-shot airborne spread burst if configured.
    /// </summary>
    private void ApplyAirborneBurst(DiseaseSymptomPrototype symptom, Entity<DiseaseCarrierComponent> ent, DiseaseData disease)
    {
        var cfg = symptom.AirborneBurst;
        if(!_prototype.TryIndex(disease.Id, out var diseaseProto))
            return;

        if ((disease.SpreadPath & DiseaseSpreadPath.Airborne) == 0)
            return;

        var range = diseaseProto.AirborneRange * MathF.Max(0.1f, cfg.RangeMultiplier);
        var mult = MathF.Max(0f, cfg.ChanceMultiplier);
        _airborneDisease.TryAirborneSpread(ent.Owner, disease, overrideRange: range, chanceMultiplier: mult);
    }
}
