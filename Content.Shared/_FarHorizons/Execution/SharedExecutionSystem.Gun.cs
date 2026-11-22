using Content.Shared.Clumsy;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Kitchen.Components;
using Content.Shared.Projectiles;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Shared.Execution;

public sealed partial class SharedExecutionSystem
{
    private const float GunDoAfterDuration = 6f;

    [Dependency] private IComponentFactory _components = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private INetManager _net = default!;

    [SubscribeLocalEvent]
    private void OnGetGunVerb(EntityUid _, GunComponent __, GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null
            || args.Using == null
            || !args.CanAccess
            || !args.CanInteract
            || !CanExecuteWithGun(args.Using.Value, args.Target, args.User))
        {
            return;
        }

        args.Verbs.Add(new UtilityVerb
        {
            Act = () => StartGunExecution(args.Using.Value, args.Target, args.User),
            Impact = LogImpact.High,
            Text = args.User == args.Target ? Loc.GetString("suicide-verb-name") : Loc.GetString("execution-verb-name"),
            Message = args.User == args.Target ? Loc.GetString("suicide-verb-message") : Loc.GetString("execution-verb-message"),
        });
    }

    private bool CanExecuteWithGun(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanBeExecuted(victim, user)
            || !TryComp<GunComponent>(weapon, out var gun)
            || !_gun.CanShoot(gun))
        {
            return false;
        }

        return !TryComp<AppearanceComponent>(weapon, out var appearance)
            || !_appearance.TryGetData(weapon, AmmoVisuals.BoltClosed, out bool boltClosed, appearance)
            || boltClosed;
    }

    private void StartGunExecution(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithGun(weapon, victim, attacker)
            || !TryComp<GunComponent>(weapon, out var gun))
        {
            return;
        }

        var attempt = new ShotAttemptedEvent { User = attacker, Used = (weapon, gun) };
        RaiseLocalEvent(weapon, ref attempt);
        if (attempt.Cancelled)
        {
            if (attempt.Message != null)
                _popup.PopupEntity(attempt.Message, weapon, attacker);

            return;
        }

        var self = attacker == victim;
        ShowExecutionInternalPopup(
            self ? "execution-popup-self-gun-initial-internal" : "execution-popup-gun-initial-internal",
            attacker,
            victim,
            weapon);
        ShowExecutionExternalPopup(
            self ? "execution-popup-self-gun-initial-external" : "execution-popup-gun-initial-external",
            attacker,
            victim,
            weapon);

        _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            attacker,
            GunDoAfterDuration,
            new GunExecutionDoAfterEvent(),
            weapon,
            target: victim,
            used: weapon)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    [SubscribeLocalEvent]
    private void OnGunExecution(Entity<GunComponent> gun, ref GunExecutionDoAfterEvent args)
    {
        if (!_net.IsServer)
        {
            // Cancel the handler on the client to avoid repeated runs of the gunshot sound during reconciliation
            args.Handled = true;
            return;
        }

        if (args.Handled
            || args.Cancelled
            || args.Used is not { } weapon
            || args.Target is not { } victim
            || !CanExecuteWithGun(weapon, victim, args.User))
        {
            return;
        }

        var prevention = new ShotAttemptedEvent { User = args.User, Used = (weapon, gun.Comp) };
        RaiseLocalEvent(weapon, ref prevention);
        RaiseLocalEvent(args.User, ref prevention);
        if (prevention.Cancelled)
            return;

        var attempt = new AttemptShootEvent(args.User, null);
        RaiseLocalEvent(weapon, ref attempt);
        if (attempt.Cancelled)
        {
            if (attempt.Message != null)
                _popup.PopupClient(attempt.Message, weapon, args.User);

            return;
        }

        var ammo = new List<(EntityUid? Entity, IShootable Shootable)>();
        RaiseLocalEvent(weapon, new TakeAmmoEvent(1, ammo, Transform(args.User).Coordinates, args.User));
        if (ammo.Count == 0)
        {
            _audio.PlayEntity(gun.Comp.SoundEmpty, Filter.Pvs(weapon), weapon, true, AudioParams.Default);
            ShowExecutionExternalPopup("execution-popup-gun-empty", args.User, victim, weapon);
            return;
        }

        var damage = GetExecutionDamage(ammo[0]);
        if (TryComp<ClumsyComponent>(args.User, out _) && !gun.Comp.ClumsyProof && _random.Prob(1f / 3f))
        {
            _damageable.TryChangeDamage(args.User, damage, origin: args.User);
            _audio.PlayEntity(gun.Comp.SoundGunshot, Filter.Pvs(weapon), weapon, true, AudioParams.Default);
            ShowExecutionInternalPopup("execution-popup-gun-clumsy-internal", args.User, victim, weapon);
            ShowExecutionExternalPopup("execution-popup-gun-clumsy-external", args.User, victim, weapon);
            args.Handled = true;
            return;
        }

        _damageable.TryChangeDamage(victim, damage * DamageMultiplier, true);
        _audio.PlayEntity(gun.Comp.SoundGunshot, Filter.Pvs(weapon), weapon, false, AudioParams.Default);
        var self = args.User == victim;
        ShowExecutionInternalPopup(
            self ? "execution-popup-self-gun-complete-internal" : "execution-popup-gun-complete-internal",
            args.User,
            victim,
            weapon,
            !self);
        ShowExecutionExternalPopup(
            self ? "execution-popup-self-gun-complete-external" : "execution-popup-gun-complete-external",
            args.User,
            victim,
            weapon);
        args.Handled = true;
    }

    private DamageSpecifier GetExecutionDamage((EntityUid? Entity, IShootable Shootable) ammo)
    {
        DamageSpecifier damage = new();
        switch (ammo.Shootable)
        {
            case CartridgeAmmoComponent cartridge:
                var prototype = _prototypes.Index<EntityPrototype>(cartridge.Prototype);
                if (prototype.TryGetComponent<HitscanBasicDamageComponent>(out var hitscan, _components))
                    damage = hitscan.Damage;
                else if (prototype.TryGetComponent<ProjectileComponent>(out var projectile, _components))
                    damage = projectile.Damage;

                if (prototype.TryGetComponent<ProjectileSpreadComponent>(out var spread, _components))
                    damage *= spread.Count;

                cartridge.Spent = true;
                _appearance.SetData(ammo.Entity!.Value, AmmoVisuals.Spent, true);
                Dirty(ammo.Entity.Value, cartridge);
                break;

            case AmmoComponent:
                if (TryComp<HitscanBasicDamageComponent>(ammo.Entity, out var ammoHitscan))
                    damage = ammoHitscan.Damage;
                else if (TryComp<ProjectileComponent>(ammo.Entity, out var ammoProjectile))
                    damage = ammoProjectile.Damage;

                Del(ammo.Entity);
                break;

            case HitscanAmmoComponent:
                if (TryComp<HitscanBasicDamageComponent>(ammo.Entity, out var basicDamage))
                    damage = basicDamage.Damage;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return damage;
    }
}
