using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [MustCallBase]
    protected virtual void InitializeFhExt()
    {
        SubscribeLocalEvent<BallisticAmmoProviderComponent, DamageExamineEvent>(OnAmmoProviderDamageExamined);
    }

    private void OnAmmoProviderDamageExamined(Entity<BallisticAmmoProviderComponent> ent, ref DamageExamineEvent args)
    {
        if (ent.Comp.Entities.FirstOrNull() is { } spawnedAmmo)
        {
            RaiseLocalEvent(spawnedAmmo, ref args);
            return;
        }

        DamageSpecifier? dmg = null;

        if (ent.Comp.UnspawnedCount > 0 && ent.Comp.Proto != null &&
            ProtoManager.TryIndex(ent.Comp.Proto, out var cartrige) &&
            cartrige.TryGetComponent<CartridgeAmmoComponent>(out var cartrigeComp, Factory))
            dmg = GetProjectileDamage(cartrigeComp.Prototype) ?? GetHitscanDamage(cartrigeComp.Prototype);
        
        if (dmg == null)
            return;

        _damageExamine.AddDamageExamine(args.Message, Damageable.ApplyUniversalAllModifiers(dmg), Loc.GetString("damage-projectile"));
    }

    private DamageSpecifier? GetHitscanDamage(EntProtoId proto)
    {
        if (!ProtoManager.TryIndex(proto, out var entityProto))
            return null;

        if (!entityProto.TryGetComponent<HitscanBasicDamageComponent>(out var hitscan, Factory))
            return null;

        if (!hitscan.Damage.Empty)
            return hitscan.Damage * Damageable.UniversalProjectileDamageModifier;

        return null;
    }
}