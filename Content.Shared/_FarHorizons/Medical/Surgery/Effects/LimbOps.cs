using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.Effects;

public sealed partial class SurgeryDetachLimb : ISurgeryStepEffect
{
    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (limb == null)
            return;
        
        var container = entMan.System<SharedContainerSystem>();
        container.TryRemoveFromContainer(limb.Value);
    }
}

public sealed partial class SurgeryLimbHealDamage : ISurgeryStepEffect
{
    [DataField(required: true)] public List<ProtoId<DamageTypePrototype>> Types;
    [DataField(required: true)] public float BaseHealing;
    [DataField(required: true)] public float ProportionalHealing;
    
    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        var targetEnt = limb ?? target;

        if (!entMan.TryGetComponent<DamageableComponent>(targetEnt, out var damageable))
            return;
        
        var dmgSys = entMan.System<DamageableSystem>();
        var damage = dmgSys.GetPositiveDamage((targetEnt, damageable));
        
        var toHeal = new DamageSpecifier();

        foreach (var type in Types)
        {
            if (!damage.DamageDict.TryGetValue(type, out var hasDamage) ||
                hasDamage <= 0)
                continue;
            
            toHeal.DamageDict.Add(type, BaseHealing + (hasDamage * ProportionalHealing));
        }

        if (!toHeal.AnyPositive())
            return;

        dmgSys.ChangeDamage((targetEnt, damageable), toHeal.Invert());
    }
}