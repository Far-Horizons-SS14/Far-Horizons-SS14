using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.Conditions;

public sealed partial class SurgeryLimbHasStatus : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }
    [DataField(required: true)] public List<ProtoId<LimbStatusEffectPrototype>> Statuses;

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        var targetEnt = limb ?? target;
        var hasAny = false;

        foreach (var step in Statuses)
        {
            if (!limbStatus.HasStatus(targetEnt, step))
                continue;

            hasAny = true;
            break;
        }

        return hasAny ^ Inverse;
    }
}

public sealed partial class SurgeryLimbHasDamage : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }
    [DataField(required: true)] public List<ProtoId<DamageTypePrototype>> Types;

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        var targetEnt = limb ?? target;

        if (!entMan.TryGetComponent<DamageableComponent>(targetEnt, out var damageable))
            return Inverse;
        
        var damage = entMan.System<DamageableSystem>().GetPositiveDamage((targetEnt, damageable));

        if (!damage.AnyPositive())
            return Inverse;

        var hasAny = false;

        foreach (var type in Types)
        {
            if (!damage.DamageDict.TryGetValue(type, out var amout) ||
                amout <= 0)
                continue;

            hasAny = true;
            break;
        }

        return hasAny ^ Inverse;
    }
}