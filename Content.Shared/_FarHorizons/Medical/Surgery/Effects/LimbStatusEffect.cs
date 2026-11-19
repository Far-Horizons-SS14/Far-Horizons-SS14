using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.Effects;

public sealed partial class SurgeryApplyLimbStatus : ISurgeryStepEffect
{
    [DataField(required: true)] public List<ProtoId<LimbStatusEffectPrototype>> Statuses;

    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        var targetEnt = limb ?? target;

        foreach (var status in Statuses)
            limbStatus.SetStatusEffect(targetEnt, status);
    }
}

public sealed partial class SurgeryRemoveLimbStatus : ISurgeryStepEffect
{
    [DataField(required: true)] public List<ProtoId<LimbStatusEffectPrototype>> Statuses;

    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        var targetEnt = limb ?? target;

        foreach (var status in Statuses)
            limbStatus.RemoveStatusEffect(targetEnt, status);
    }
}