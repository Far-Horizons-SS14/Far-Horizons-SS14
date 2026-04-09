using System.Linq;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Healing;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.LimbDamage;

public partial class LimbDamageSystem
{
    public static ProtoId<OrganCategoryPrototype> DefaultLimb = "Torso";

    public bool TryChangeLimbDamage(Entity<LimbDamageableComponent?> target, ProtoId<OrganCategoryPrototype> targetLimb,
        DamageSpecifier damage, out DamageSpecifier bodyDamageDealt, bool ignoreResistances = false,
        bool interruptsDoAfters = true, EntityUid? origin = null, bool ignoreGlobalModifiers = false,
        float armorPenetration = 0, bool canHeal = true, bool skipBodyDamage = false) => TryChangeLimbDamage(target, targetLimb, damage,
        out bodyDamageDealt, out _, ignoreResistances, interruptsDoAfters, origin, ignoreGlobalModifiers,
        armorPenetration, canHeal, skipBodyDamage);

    public bool TryChangeLimbDamage(Entity<LimbDamageableComponent?> target, ProtoId<OrganCategoryPrototype> targetLimb,
        DamageSpecifier damage, out DamageSpecifier bodyDamageDealt, out DamageSpecifier limbDamageDealt, bool ignoreResistances = false,
        bool interruptsDoAfters = true, EntityUid? origin = null, bool ignoreGlobalModifiers = false,
        float armorPenetration = 0, bool canHeal = true, bool skipBodyDamage = false)
    {
        bodyDamageDealt = new();
        limbDamageDealt = new();

        if (!Resolve(target, ref target.Comp, false) ||
            target.Comp.DefaultLimb == targetLimb)
            return false; // Damage to torso is vanilla damage system which we don't handle.
        
        var targetLimbEnt = GetAllDamageable(target).Where(p => p.Comp.Organ!.Category == targetLimb).FirstOrNull();
        if (targetLimbEnt == null)
            return false;

        limbDamageDealt = _damageable.ChangeDamage(targetLimbEnt.Value.Owner, damage, ignoreResistances,
            interruptsDoAfters, origin, ignoreGlobalModifiers, armorPenetration, canHeal);

        if (!skipBodyDamage)
            bodyDamageDealt = _damageable.ChangeDamage(target.Owner,
                limbDamageDealt * targetLimbEnt.Value.Comp.BodyDamageFactor, true, interruptsDoAfters, origin,
                ignoreGlobalModifiers, armorPenetration, canHeal);

        return true;
    }

    public void ChangeDamageAll(Entity<LimbDamageableComponent?> target, DamageSpecifier damage, bool ignoreResistances = false, bool interruptsDoAfters = true, EntityUid? origin = null, bool ignoreGlobalModifiers = false, float armorPenetration = 0, bool canHeal = true)
    {
        if (!Resolve(target, ref target.Comp, false))
            return;

        var allLimbs = GetAllDamageable(target);
        foreach (var limb in allLimbs)
            _damageable.ChangeDamage(limb.Owner, damage, ignoreResistances, interruptsDoAfters, origin, ignoreGlobalModifiers, armorPenetration, canHeal);
    }

    public DamageSpecifier HealAllEvenly(Entity<LimbDamageableComponent?> target, FixedPoint2 amount,
        ProtoId<DamageGroupPrototype>? group = null, EntityUid? origin = null)
    {
        DamageSpecifier totalHealed = new();

        if (!Resolve(target, ref target.Comp, false))
            return totalHealed;

        var allLimbs = GetAllDamageable(target);
        foreach (var limb in allLimbs)
            totalHealed += _damageable.HealEvenly(limb.Owner, amount, group, origin);
        return totalHealed;
    }

    public void ClearAllDamage(Entity<LimbDamageableComponent?> target)
    {
        if (!Resolve(target, ref target.Comp, false)) return;

        var allLimbs = GetAllDamageable(target);
        foreach (var limb in allLimbs)
            _damageable.ClearAllDamage((limb.Owner, limb.Comp.Damageable));
    }

    public bool LimbHasDamage(Entity<LimbDamageableComponent?> target, ProtoId<OrganCategoryPrototype> targetLimb, HealingComponent healing)
    {
        var limb = GetAllDamageable(target).Where(p => p.Comp.Organ!.Category == targetLimb).FirstOrNull();
        if (limb?.Comp.Damageable == null)
            return false;

        foreach (var (k, _) in _damageable.GetPositiveDamage((limb.Value.Owner, limb.Value.Comp.Damageable)).DamageDict)
            if (healing.Damage.DamageDict.ContainsKey(k))
                return true;
        return false;
    }

    public bool CheckAttackHit(Entity<LimbDamageableComponent?> target, ProtoId<OrganCategoryPrototype> targetLimb, out ProtoId<OrganCategoryPrototype>? overrideTarget)
    {
        overrideTarget = null;

        if (!Resolve(target, ref target.Comp, false))
            return false;

        var ev = new LimbHitCheckEvent(targetLimb);
        RaiseLocalEvent(target, ref ev);
        
        overrideTarget = ev.HitTarget;
        return overrideTarget != null;
    }

    public bool CheckAttackHit(Entity<LimbDamageableComponent?> target, Entity<LimbTargettingComponent?> source, out ProtoId<OrganCategoryPrototype>? overrideTarget)
    {
        overrideTarget = null;

        if (!Resolve(target, ref target.Comp, false) ||
            !Resolve(source, ref source.Comp, false)) 
            return true; // No missing when attack shouldn't be handled by limb damage
        
        if (source.Comp.Target == target.Comp.DefaultLimb)
        {
            overrideTarget = source.Comp.Target;
            return true;
        }
        
        return CheckAttackHit(target, source.Comp.Target, out overrideTarget);
    }

    public ProtoId<OrganCategoryPrototype>? TryScatterHitTarget(Entity<LimbDamageableComponent?> target, ProtoId<OrganCategoryPrototype>? aimedTowards = null)
    {
        if (!Resolve(target, ref target.Comp, false))
            return null;

        var ev = new LimbScatterHitTargetCheckEvent(aimedTowards);
        RaiseLocalEvent(target, ref ev);

        return ev.Handled ? ev.Target : null;
    }

    public ProtoId<OrganCategoryPrototype>? TryScatterHitTarget(Entity<LimbDamageableComponent?> target,
        Entity<LimbTargettingComponent?> source)
    {
        if (!Resolve(target, ref target.Comp, false) ||
            !Resolve(source, ref source.Comp, false))
            return null;

        return TryScatterHitTarget(target, source.Comp.Target);
    }

    public List<Entity<DamageableLimbComponent>> GetAllDamageable(Entity<LimbDamageableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false) ||
            ent.Comp.Body is not { Organs: not null })
            return new();

        return ent.Comp.Body.Organs.ContainedEntities
            .Select(p => (Entity<DamageableLimbComponent?>)(p, CompOrNull<DamageableLimbComponent>(p)))
            .Where(p => p.Comp is { Organ: not null })
            .Select(p => (Entity<DamageableLimbComponent>)(p, p.Comp!)).ToList();
    }

    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>>?
        TryGetFullBodyDamage(Entity<DamageableComponent?, LimbDamageableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, false))
            return null;

        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>> damage =
            new() { [DefaultLimb] = _damageable.GetPositiveDamage((ent, ent.Comp1)).DamageDict };

        if (!Resolve(ent, ref ent.Comp2, false))
            return damage;

        var allDamageable = GetAllDamageable((ent, ent.Comp2));
        foreach (var limb in allDamageable)
            if (limb.Comp.Organ!.Category != null &&
                limb.Comp.Damageable != null)
                damage[limb.Comp.Organ!.Category.Value] = _damageable.GetPositiveDamage((limb.Owner, limb.Comp.Damageable)).DamageDict;
        
        return damage;
    }

    public Dictionary<ProtoId<OrganCategoryPrototype>, IReadOnlyDictionary<ProtoId<DamageGroupPrototype>, FixedPoint2>>?
        TryGetFullBodyDamageGroups(Entity<DamageableComponent?, LimbDamageableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, false))
            return null;

        Dictionary<ProtoId<OrganCategoryPrototype>, IReadOnlyDictionary<ProtoId<DamageGroupPrototype>, FixedPoint2>>
            damage = new() { [DefaultLimb] = _damageable.GetDamagePerGroup((ent, ent.Comp1)) };
        

        if (!Resolve(ent, ref ent.Comp2, false))
            return damage;

        var allDamageable = GetAllDamageable((ent, ent.Comp2));
        foreach (var limb in allDamageable)
            if (limb.Comp.Organ!.Category != null &&
                limb.Comp.Damageable != null)
                damage[limb.Comp.Organ!.Category.Value] = _damageable.GetDamagePerGroup((limb.Owner, limb.Comp.Damageable));
        
        return damage;
    }

    public ProtoId<OrganCategoryPrototype>? GetCurrentSelectedTarget(Entity<LimbTargettingComponent?> source)
    {
        if (!Resolve(source, ref source.Comp)) return null;

        return source.Comp.Target;
    }

    public ProtoId<OrganCategoryPrototype>? GetCurrentValidTarget(Entity<LimbDamageableComponent?> target,
        Entity<LimbTargettingComponent?> source)
    {
        if (!Resolve(target, ref target.Comp)) return null;

        var selectedTarget = GetCurrentSelectedTarget(source);
        if (selectedTarget == null)
            return null;

        var allDamageable = GetAllDamageable(target);
        return allDamageable.Any(p => p.Comp.Organ!.Category == selectedTarget) ? selectedTarget : null;
    }

    public void SetGodMode(Entity<LimbDamageableComponent?> target, bool state)
    {
        if (!Resolve(target, ref target.Comp)) return;

        foreach (var limb in GetAllDamageable(target))
        {
            if (state)
                EnsureComp<GodmodeComponent>(limb);
            else
                RemCompDeferred<GodmodeComponent>(limb);
        }
    }

    public void SetChangelingLimbs(Entity<LimbDamageableComponent?> target, bool state)
    {
        if (!Resolve(target, ref target.Comp, false)) return;

        foreach (var limb in GetAllDamageable(target))
        {
            if (state)
                EnsureComp<ChangelingLimbComponent>(limb);
            else
                RemCompDeferred<ChangelingLimbComponent>(limb);
        }
    }
}