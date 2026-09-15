using Content.Shared.EntityEffects;
using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Content.Shared._FarHorizons.Medical.Disease.Components;
using Robust.Shared.Timing;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using System.Linq;
using Robust.Shared.Network;

namespace Content.Shared._FarHorizons.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomStatusEffect : SymptomBehavior
{
    /// <summary>
    /// List of effects to execute on symptom trigger. Supports any <see cref="EntityEffect"/>.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects { get; private set; } = [];
}

public sealed partial class SymptomStatusEffect
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private INetManager _net = default!;

    /// <summary>
    /// Executes the status effects.
    /// </summary>
    public override void OnSymptom(Entity<DiseaseCarrierComponent> entity, DiseaseData disease, StageData stage,  DiseaseSymptomPrototype symptom)
    {
        if (Effects.Length == 0  || _net.IsClient)
            return;

        foreach(var condition in Conditions)
            if(!condition.Check(entity, disease, stage))
                return;

        var probOverride = disease.Symptoms.FirstOrDefault(p => p.Symptom.Id == symptom.ID);
        if(probOverride != null && probOverride.Probability.TryGetValue(stage.Stage, out var stageProb))
            Probability = stageProb;
        var seed = SharedRandomExtensions.HashCodeCombine(
            _timing.CurTime.Microseconds,
            stage.AdvanceStageAt.Microseconds,
            stage.Stage,
            _entMan.GetNetEntity(entity.Owner).Id,
            symptom.ID.GetHashCode(),
            Index
        );        
        var rand = new System.Random(seed);
        if(!rand.Prob(Probability))
            return;

        _effects.ApplyEffects(entity, Effects);
    }
}
