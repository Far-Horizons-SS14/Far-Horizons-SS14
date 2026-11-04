using Content.Shared.Damage.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Mobs.Components;
using Content.Shared._Starlight.Actions.Events;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Stunnable;
using Content.Shared._FarHorizons.ReagentDraw;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.PowerCell.Components;
using Content.Shared.Hands;
using Content.Shared.Mobs;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Damage.Systems;

namespace Content.Shared._FarHorizons.Vehicles;

public abstract partial class SharedVehicleSystem
{   
    [Dependency] protected SharedStaminaSystem _stamina = default!;
    public void InitializeRider()
    {
        SubscribeLocalEvent<RiderComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<RiderComponent, KnockedDownEvent>(OnKnockdown);
        SubscribeLocalEvent<RiderComponent, UpdateCanMoveEvent>(OnUpdateCanMoveEvent);
        SubscribeLocalEvent<RiderComponent, JumpActionEvent>(OnJumpActionEvent);
        SubscribeLocalEvent<RiderComponent, ShooterImpulseEvent>(OnShooterEvent);
        SubscribeLocalEvent<RiderComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<RiderComponent, EntityTerminatingEvent>(OnRiderTerminating);
    }
    private void OnStunned(Entity<RiderComponent> ent, ref StunnedEvent args)
    { 
        var vehicle = ent.Comp.Riding;
        if(!TryComp<VehicleBuckleComponent>(vehicle, out var vehicleBuckleComp)) return;
        if(!vehicleBuckleComp.stundismount) return;
        if(!TryComp<BuckleComponent>(ent.Owner, out var buckleComp)) return;

        _buckle.Unbuckle((ent.Owner, buckleComp), ent.Owner);   
    }

    private void OnKnockdown(Entity<RiderComponent> ent, ref KnockedDownEvent args)
    {
        var vehicle = ent.Comp.Riding;
        if(!TryComp<VehicleBuckleComponent>(vehicle, out var vehicleBuckleComp)) return;
        if(!vehicleBuckleComp.knockdowndismount) return;
        if(!TryComp<BuckleComponent>(ent.Owner, out var buckleComp)) return;
        
        _buckle.Unbuckle((ent.Owner, buckleComp), ent.Owner);   
    }
    
    private void OnJumpActionEvent(Entity<RiderComponent> ent, ref JumpActionEvent args)
    {
        if(!TryComp<BuckleComponent>(ent.Owner, out var buckleComp)) return;
        _buckle.Unbuckle((ent.Owner, buckleComp), ent.Owner);
    }

    private void OnUpdateCanMoveEvent(Entity<RiderComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (!TryComp<VehicleComponent>(ent.Comp.Riding, out var vehicleComp))
            return;

        if (vehicleComp.RequireIgnition && !vehicleComp.Started)
        {
            args.Cancel();
            return;
        }

        if (ent.Comp.Riding == null)
            return;

        var riding = ent.Comp.Riding.Value;

        TryComp<PowerCellDrawComponent>(riding, out var pcdComp);
        TryComp<ReagentDrawComponent>(riding, out var rdComp);

        var noPower =
            (vehicleComp.CellPowered && pcdComp != null && !_powerCell.HasDrawCharge(riding)) ^
            (!vehicleComp.CellPowered && rdComp != null && !_reagentDraw.HasDrawReagent(riding));

        if (!noPower) return;

        if (vehicleComp.Started)
            vehicleComp.Started = false;

        if (vehicleComp.CellPowered && pcdComp?.Enabled == vehicleComp.Started)
        {
            _powerCell.SetDrawEnabled((riding, pcdComp), false);
        }

        if (!vehicleComp.CellPowered && rdComp?.Enabled == true)
        {
            rdComp.Enabled = vehicleComp.Started;
            _ambientSound.SetAmbience(riding, false);
            Dirty(riding, rdComp);
        }

        Dirty(riding, vehicleComp);
        args.Cancel();
    }

    private void OnShooterEvent(Entity<RiderComponent> ent, ref ShooterImpulseEvent args)
    {
        if(!TryComp<StaminaComponent>(ent.Owner, out var stamina)) return;
        if(ent.Comp.Riding != null && TryComp<VehicleComponent>(ent.Comp.Riding.Value, out var vehicleComp) && !vehicleComp.AllowGunKnockback) return;

        foreach(var held in _handsSystem.EnumerateHeld(ent.Owner))
        {
            if(HasComp<GunComponent>(held) && HasComp<WieldableComponent>(held))
            {
                _stamina.TakeStaminaDamage(ent.Owner, stamina.CritThreshold*0.10f, component: stamina);
            }
        }
    }

    private void OnPullAttempt(Entity<RiderComponent> ent, ref PullAttemptEvent args)
    {
        if(TryComp<MobStateComponent>(ent.Owner, out var mbState) 
        && (mbState.CurrentState == MobState.Critical 
            || mbState.CurrentState == MobState.ActiveCritical
            || mbState.CurrentState == MobState.Dead 
            || mbState.CurrentState == MobState.Invalid))
        {
            _buckle.Unbuckle(ent.Owner, args.PullerUid);
            return;
        }
        args.Cancelled = true;
    }

    private void OnRiderTerminating(Entity<RiderComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Riding is { } vehicle)
            RemoveRider(ent.Owner, vehicle);
    }

    [SubscribeLocalEvent]
    private void OnHandEquipped(Entity<RiderComponent> ent, ref GotEquippedHandEvent args)
    {
        if(ent.Comp.Riding is not { } vehicle) return;
        RemoveRider(ent.Owner, vehicle);
    }

    [SubscribeLocalEvent]
    private void OnHandEquippedRider(Entity<RiderComponent> ent, ref DidEquipHandEvent args)
    {
        if (TryComp<GunComponent>(args.Equipped, out var gun))
            _gun.RefreshModifiers((args.Equipped, gun));
    }

    [SubscribeLocalEvent]
    private void OnHandUnequippedRider(Entity<RiderComponent> ent, ref DidUnequipHandEvent args)
    {
        if (TryComp<GunComponent>(args.Unequipped, out var gun))
            _gun.RefreshModifiers((args.Unequipped, gun));
    }

    [SubscribeLocalEvent]
    private void OnWieldAttemptEvent(Entity<RiderComponent> ent, ref WieldAttemptEvent args)
    {
        if(ent.Comp.Riding == null || !TryComp<VehicleComponent>(ent.Comp.Riding.Value, out var vehicleComp) || !vehicleComp.DisallowWieldingGuns)
            return;

        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnGunRefreshModifiers(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var transform = Transform(ent.Owner);
        if(!TryComp<RiderComponent>(transform.ParentUid, out var riderComp) || riderComp.Riding == null) return;
        if(HasComp<PowerCellDrawComponent>(riderComp.Riding.Value) 
            ^ HasComp<ReagentDrawComponent>(riderComp.Riding.Value))
        {
            if(HasComp<VehicleContainerComponent>(riderComp.Riding.Value))
            {
                args.MinAngle += Angle.FromDegrees(30);
                args.MaxAngle += Angle.FromDegrees(30);
            }
            else if(HasComp<VehicleBuckleComponent>(riderComp.Riding.Value))
            {
                args.MinAngle += Angle.FromDegrees(20);
                args.MaxAngle += Angle.FromDegrees(20);
            }
        }
    }

    private void RefreshHeldGuns(EntityUid rider)
    {
        foreach (var held in _handsSystem.EnumerateHeld(rider))
        {
            if (TryComp<GunComponent>(held, out var gun))
                _gun.RefreshModifiers((held, gun));
        }
    }
}