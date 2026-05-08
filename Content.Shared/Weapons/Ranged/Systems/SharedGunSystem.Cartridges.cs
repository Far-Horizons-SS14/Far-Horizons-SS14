using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Projectiles;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [Dependency] private readonly DamageExamineSystem _damageExamine = default!;

    // needed for server system
    protected virtual void InitializeCartridge()
    {
        SubscribeLocalEvent<CartridgeAmmoComponent, ExaminedEvent>(OnCartridgeExamine);
        SubscribeLocalEvent<CartridgeAmmoComponent, DamageExamineEvent>(OnCartridgeDamageExamine);
    }

    private void OnCartridgeExamine(Entity<CartridgeAmmoComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(ent.Comp.Spent
            ? Loc.GetString("gun-cartridge-spent")
            : Loc.GetString("gun-cartridge-unspent"));
        
        // Far Horizons start
        var proto = ProtoManager.Index(ent.Comp.Prototype);
        if (!proto.Components.TryGetValue("EmpOnTrigger", out var empReg) ||
            empReg.Component is not EmpOnTriggerComponent empComp)
            return;
        
        if (args.IsInDetailsRange)
            args.PushText(Loc.GetString("emp-grenade-strength-description", ("empStrength", empComp.Strength)), 10);
        // Far Horizons end
    }

    private void OnCartridgeDamageExamine(Entity<CartridgeAmmoComponent> ent, ref DamageExamineEvent args)
    {
        var damageSpec = GetProjectileDamage(ent.Comp.Prototype) ?? GetHitscanDamage(ent.Comp.Prototype); // Far Horizons

        if (damageSpec == null)
            return;

        _damageExamine.AddDamageExamine(args.Message, Damageable.ApplyUniversalAllModifiers(damageSpec), Loc.GetString("damage-projectile"));
    }

    private DamageSpecifier? GetProjectileDamage(EntProtoId proto)
    {
        if (!ProtoManager.TryIndex(proto, out var entityProto))
            return null;

        if (!entityProto.TryGetComponent<ProjectileComponent>(out var projectile, Factory))
            return null;

        if (!projectile.Damage.Empty)
            return projectile.Damage * Damageable.UniversalProjectileDamageModifier;

        return null;
    }
}
