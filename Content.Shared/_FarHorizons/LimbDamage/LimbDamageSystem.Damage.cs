using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Hitscan.Systems;

namespace Content.Shared._FarHorizons.LimbDamage;

public partial class LimbDamageSystem
{

    private void InitDamage()
    {
        SubscribeLocalEvent<LimbAimedHitscanShotComponent, HitscanRaycastFiredEvent>(OnAimedShotHit, before: [typeof(HitscanBasicDamageSystem)]);
        SubscribeLocalEvent<DamageableLimbComponent, OrganGotInsertedEvent>(OnDamageableLimbInserted);
        SubscribeLocalEvent<DamageableLimbComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<DamageableLimbComponent, BodyRelayedEvent<RejuvenateEvent>>(OnRejuvenateBody);
        SubscribeLocalEvent<ChangelingLimbComponent, DamageChangedEvent>(OnChangelingLimbDamaged);
    }

    private void OnRejuvenate(Entity<DamageableLimbComponent> ent, ref RejuvenateEvent args) => 
        _damageable.SetAllDamage((ent, ent.Comp.Damageable), 0);

    private void OnRejuvenateBody(Entity<DamageableLimbComponent> ent, ref BodyRelayedEvent<RejuvenateEvent> args) => 
        _damageable.SetAllDamage((ent, ent.Comp.Damageable), 0);

    private void OnAimedShotHit(Entity<LimbAimedHitscanShotComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.DamageHandled ||
            args.Data.HitEntity == null ||
            !TryComp<HitscanBasicDamageComponent>(ent, out var damageComp))
            return;

        var dmg = damageComp.Damage * _damageable.UniversalHitscanDamageModifier;
        if (!TryChangeLimbDamage(args.Data.HitEntity.Value, ent.Comp.Target, dmg, out var damageDealt,
                damageComp.IgnoreResistances, true, args.Data.Shooter, false, damageComp.ArmorPenetration, false, false, true))
            return;

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = args.Data.HitEntity.Value,
            DamageDealt = damageDealt,
            Data = args.Data
        };

        RaiseLocalEvent(ent, ref damageEvent);
        args.DamageHandled = true;
    }

    private void OnDamageableLimbInserted(Entity<DamageableLimbComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!TryComp<DamageableComponent>(args.Target, out var bodyDamageable)) return;
        var damageable = EnsureComp<DamageableComponent>(ent);
        _damageable.SetDamageModifierSetId((ent, damageable), bodyDamageable.DamageModifierSetId);
    }
    
    private void OnChangelingLimbDamaged(Entity<ChangelingLimbComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        var allDamage = _damageable.GetPositiveDamage((ent, args.Damageable));
        allDamage.ClampMax(ent.Comp.DamageCap);
        _damageable.SetDamage((ent, args.Damageable), allDamage);
    }
}