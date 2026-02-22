using Content.Server.Power.Components;
using Content.Shared._FarHorizons.Shuttles;
using Content.Shared.Abilities.Goliath;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Shuttles;

public sealed class TurretControlServerSystem : AccUpdateEntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurretControlServerComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<TurretControlServerComponent, AnchorStateChangedEvent>(OnAnchor);
        
        SubscribeLocalEvent<TurretControlServerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnInit(EntityUid uid, TurretControlServerComponent comp, ref MapInitEvent args) => GetTurrets(uid, comp);

    private void OnAnchor(EntityUid uid, TurretControlServerComponent comp, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            GetTurrets(uid, comp);
        }
        else
        {
            ClearTurrets(comp);
        }
    }

    private void OnGetVerbs(EntityUid uid, TurretControlServerComponent comp, ref GetVerbsEvent<Verb> args)
    {
        if(!args.CanAccess)
            return;

        if(!TryComp(uid, out TransformComponent? xform) || !xform.Anchored)
            return;
        
        args.Verbs.Add(new()
        {
            Text = Loc.GetString("turret-control-server-verb-refresh-name"),
            Act = () => GetTurrets(uid, comp),
            Message = Loc.GetString("turret-control-server-verb-refresh-description")
        });
    }

    private void GetTurrets(EntityUid uid, TurretControlServerComponent comp)
    {
        // Maybe a redundant check, but it never hurts to make sure
        if(!TryComp(uid, out TransformComponent? xform) || !xform.Anchored)
            return;
        
        if(xform.GridUid == null)
            return;

        // Free all the turrets we were already controlling
        ClearTurrets(comp);

        var query = EntityQueryEnumerator<ControlledTurretComponent, TransformComponent>();
        while (query.MoveNext(out var turretUid, out var turretComp, out var transform))
        {
            // Turret isn't a part of our grid
            if(xform.GridUid != transform.GridUid || !transform.Anchored)
                continue;

            // Turret is already claimed
            if(turretComp.ControlServer != null)
            {
                //... by us? Probably an admin's fault
                if(turretComp.ControlServer == (uid, comp))
                    comp.Turrets.Add((turretUid, turretComp));
                
                continue;
            }

            turretComp.ControlServer = (uid, comp);
            comp.Turrets.Add((turretUid, turretComp));
        }
    }

    private static void ClearTurrets(TurretControlServerComponent comp)
    {
        foreach (var (_, turret) in comp.Turrets)
        {
            turret.TargetRotation = null;
            turret.TargetCoordinates = null;
            turret.ControlServer = null;
        }
        comp.Turrets = [];
    }

    protected override void AccUpdate()
    {
        var query = EntityQueryEnumerator<TurretControlServerComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var comp, out var powerReceiver))
        {
            if(!powerReceiver.Powered)
                return;

            if(comp.InvalidTurrets.Count > 0)
            {
                foreach(var turret in comp.InvalidTurrets)
                    comp.Turrets.Remove(turret);
                comp.InvalidTurrets = [];
            }

            foreach(var (_, turret) in comp.Turrets)
            {
                if(turret.TargetCoordinates == comp.TargetCoordinates)
                    continue;
                
                turret.TargetCoordinates = comp.TargetCoordinates;
            }
        }
    }

    public void Fire(EntityUid uid, TurretControlServerComponent comp)
    {
        if(!TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver) || !powerReceiver.Powered)
            return;

        foreach (var (turretUid, turret) in comp.Turrets)
        {
            if(turret.State != ControlledTurretFiringState.Ready)
                continue;
            
            if(!TryComp<GunComponent>(turretUid, out var gun) || _gunSystem.GetAmmoCount(turretUid) < 1)
                continue;

            if (_gunSystem.CanShoot(gun))
            {
                Timer.Spawn(_random.Next(300), () => _gunSystem.AttemptShoot(turretUid, gun));
                turret.State = ControlledTurretFiringState.Firing;
            }
        }
    }
}