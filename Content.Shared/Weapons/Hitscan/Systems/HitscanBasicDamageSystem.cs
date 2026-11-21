using Content.Shared._FarHorizons.LimbDamage;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed partial class HitscanBasicDamageSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private LimbDamageSystem _limbDamage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null ||
            args.DamageHandled) // Far Horisons
            return;

        var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier;

        // Far Horizons limb targetting start
        ProtoId<OrganCategoryPrototype>? limbTarget = null;
        if (args.Data.Shooter != null)
            limbTarget = _limbDamage.TryScatterHitTarget(args.Data.HitEntity.Value, args.Data.Shooter.Value);

        DamageSpecifier damageDealt;
        if (limbTarget != null)
            _limbDamage.TryChangeLimbDamage(
                args.Data.HitEntity.Value,
                limbTarget.Value,
                dmg,
                out damageDealt,
                ignoreResistances: ent.Comp.IgnoreResistances,
                origin: args.Data.Shooter!.Value,
                armorPenetration: ent.Comp.ArmorPenetration,
                canHeal: false
            );
        else
            // var damageDealt = _damage.TryChangeDamage(args.Data.HitEntity.Value, dmg, origin: args.Data.Gun); // Starlight - we redefine this
            // Starlight start
            damageDealt = _damage.ChangeDamage(
                args.Data.HitEntity.Value,
                dmg,
                ignoreResistances: ent.Comp.IgnoreResistances,
                origin: args.Data.Gun,
                armorPenetration: ent.Comp.ArmorPenetration,
                canHeal: false
            );
            // Starlight end
        // Far Horizons end
        

        if (damageDealt == null)
            return;

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = args.Data.HitEntity.Value,
            DamageDealt = damageDealt,
            Data = args.Data, // Starlight
        };

        RaiseLocalEvent(ent, ref damageEvent);

        args.DamageHandled = true; // Far Horisons
    }
}
