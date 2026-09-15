using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;
using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Network;

namespace Content.Shared._FarHorizons.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomGenericStatusEffect : SymptomBehavior
{
    /// <summary>
    /// Prototype ID of the status effect entity to apply. Must be an entity with <see cref="StatusEffectComponent"/>.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EffectProto { get; private set; }

    /// <summary>
    /// Duration in seconds for the status effect. Behavior depends on <see cref="Refresh"/> and <see cref="Type"/>.
    /// </summary>
    [DataField]
    public float Time { get; private set; } = 4.0f;

    /// <summary>
    /// true - refresh to greater value; false - accumulate.
    /// Only used when <see cref="Type"/> is Add.
    /// </summary>
    [DataField]
    public bool Refresh { get; private set; } = true;

    /// <summary>
    /// How to modify the status effect time <see cref="StatusEffectSymptomType"/>.
    /// </summary>
    [DataField]
    public StatusEffectSymptomType Type { get; private set; } = StatusEffectSymptomType.Add;
}

public sealed partial class SymptomGenericStatusEffect
{
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityManager _entMan = default!;
    [Dependency] private INetManager _net = default!;

    /// <summary>
    /// Adds an effect status component to the entity.
    /// </summary>
    public override void OnSymptom(Entity<DiseaseCarrierComponent> entity, DiseaseData disease, StageData stage, DiseaseSymptomPrototype symptom)
    {
        if(_net.IsClient)
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

        var duration = TimeSpan.FromSeconds(Time);

        switch (Type)
        {
            case StatusEffectSymptomType.Add:
                if (Refresh)
                    _status.TryUpdateStatusEffectDuration(entity, EffectProto, duration);
                else
                    _status.TryAddStatusEffectDuration(entity, EffectProto, duration);
                break;

            case StatusEffectSymptomType.Remove:
                _status.TryAddTime(entity, EffectProto, -duration);
                break;

            case StatusEffectSymptomType.Set:
                _status.TrySetStatusEffectDuration(entity, EffectProto, duration);
                break;
        }
    }

    public enum StatusEffectSymptomType
    {
        Add,
        Remove,
        Set
    }
}
