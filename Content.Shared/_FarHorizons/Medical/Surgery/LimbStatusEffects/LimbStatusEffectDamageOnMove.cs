using System.Linq;
using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared._FarHorizons.Medical.Trauma;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.LimbStatusEffects;

public sealed partial class LimbStatusEffectDamageOnMove : LimbStatusEffectGlobal<LimbStatusEffectDamageOnMove>
{
    [DataField(required: true)] public DamageSpecifier Damage;
    [DataField] public TimeSpan RateLimit = TimeSpan.FromSeconds(1);
    [ViewVariables] public Dictionary<ProtoId<OrganCategoryPrototype>, DamageSpecifier> TotalDamage = new();

    protected override bool Eq(LimbStatusEffectDamageOnMove other)
    {
        if (RateLimit != other.RateLimit || TotalDamage.Count != other.TotalDamage.Count)
            return false;

        foreach (var (limb, dmg) in TotalDamage)
        {
            if (!other.TotalDamage.TryGetValue(limb, out var otherDmg) || !SameDamage(dmg, otherDmg))
                return false;
        }

        return true;
    }

    public override ILimbStatusEffect Clone()
    {
        var c = (LimbStatusEffectDamageOnMove) base.Clone();
        c.Damage = new DamageSpecifier(Damage);

        var total = new Dictionary<ProtoId<OrganCategoryPrototype>, DamageSpecifier>(TotalDamage.Count);
        foreach (var (limb, dmg) in TotalDamage)
            total[limb] = new DamageSpecifier(dmg);
        c.TotalDamage = total;

        return c;
    }

    protected override LimbStatusEffectDamageOnMove Stack(LimbStatusEffectDamageOnMove other)
    {
        var total = new Dictionary<ProtoId<OrganCategoryPrototype>, DamageSpecifier>(TotalDamage.Count + other.TotalDamage.Count);

        foreach (var (limb, dmg) in TotalDamage)
            MergeMax(total, limb, dmg);
        foreach (var (limb, dmg) in other.TotalDamage)
            MergeMax(total, limb, dmg);

        return new LimbStatusEffectDamageOnMove
        {
            Limb = Limb ?? other.Limb,
            TotalDamage = total,
            RateLimit = RateLimit <= other.RateLimit ? RateLimit : other.RateLimit
        };
    }

    public override void Normalize()
    {
        if (Limb is { } limb && Damage.AnyPositive())
            MergeMax(TotalDamage, limb, Damage);

        Damage = new DamageSpecifier();
    }

    private static void MergeMax(Dictionary<ProtoId<OrganCategoryPrototype>, DamageSpecifier> dst, ProtoId<OrganCategoryPrototype> limb, DamageSpecifier src)
    {
        if (!dst.TryGetValue(limb, out var existing))
        {
            dst[limb] = new DamageSpecifier(src);
            return;
        }

        foreach (var (type, value) in src.DamageDict)
            existing.DamageDict[type] = existing.DamageDict.TryGetValue(type, out var cur)
                ? FixedPoint2.Max(cur, value)
                : value;
    }

    private static bool SameDamage(DamageSpecifier a, DamageSpecifier b)
    {
        foreach (var (type, value) in a.DamageDict)
        {
            b.DamageDict.TryGetValue(type, out var other);
            if (value != other)
                return false;
        }

        foreach (var (type, value) in b.DamageDict)
        {
            if (!a.DamageDict.ContainsKey(type) && value != FixedPoint2.Zero)
                return false;
        }

        return true;
    }

    public override void Apply(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs)
    {
        var comp = entMan.EnsureComponent<SurgeryLimbStatusDamageOnMoveComponent>(target);
        comp.DamageRate = RateLimit;
        comp.Damage = TotalDamage.ToDictionary(p => p.Key, p => new DamageSpecifier(p.Value));
        entMan.Dirty(target, comp);
    }
    public override void Remove(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs) =>
        entMan.RemoveComponentDeferred<SurgeryLimbStatusDamageOnMoveComponent>(target);

    public override void Update(IEntityManager entMan, EntityUid target, ILimbStatusEffect old, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs) => 
        Apply(entMan, target, organs);
}