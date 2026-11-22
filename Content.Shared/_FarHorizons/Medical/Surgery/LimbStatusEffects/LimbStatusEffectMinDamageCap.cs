using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared._FarHorizons.Medical.Trauma;
using Content.Shared.Body;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.LimbStatusEffects;

public sealed partial class LimbStatusEffectMinDamageCap : LimbStatusEffectLocal<LimbStatusEffectMinDamageCap>
{
    [DataField(required: true)] public DamageSpecifier MinDamageCap;

    protected override bool Eq(LimbStatusEffectMinDamageCap other) => MinDamageCap == other.MinDamageCap;

    public override ILimbStatusEffect Clone()
    {
        var c = (LimbStatusEffectMinDamageCap) base.Clone();
        c.MinDamageCap = new DamageSpecifier(MinDamageCap);
        return c;
    }

    protected override LimbStatusEffectMinDamageCap Stack(LimbStatusEffectMinDamageCap other)
    {
        var total = new DamageSpecifier(MinDamageCap);

        foreach (var (type, value) in other.MinDamageCap.DamageDict)
            if (!total.DamageDict.TryGetValue(type, out var cur) || value > cur)
                total.DamageDict[type] = value;

        return new LimbStatusEffectMinDamageCap
        {
            Limb = Limb ?? other.Limb,
            MinDamageCap = total
        };

    }

    public override void Apply(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs)
    {
        if (!TryGetTarget(target, organs, out var limb) || entMan.Deleted(limb))
            return;

        var comp = entMan.EnsureComponent<SurgeryLimbStatusMinDamageCapComponent>(limb);
        comp.MinDamageCap = MinDamageCap;
        entMan.Dirty(limb, comp);
    }
    public override void Remove(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs)
    {
        if (!TryGetTarget(target, organs, out var limb) || entMan.Deleted(limb))
            return;
        
        entMan.RemoveComponentDeferred<SurgeryLimbStatusMinDamageCapComponent>(limb);
    }
    public override void Update(IEntityManager entMan, EntityUid target, ILimbStatusEffect old, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs) => 
        Apply(entMan, target, organs);
}