using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Wieldable.Components;
using Content.Shared.Wieldable;
using Content.Shared.Clothing;
using Content.Shared.Inventory;

namespace Content.Shared._FarHorizons.Vehicles;

public abstract partial class SharedVehicleSystem
{    
    public const SlotFlags WeightSlots =
        SlotFlags.FEET |
        SlotFlags.HEAD |
        SlotFlags.EYES |
        SlotFlags.GLOVES |
        SlotFlags.MASK |
        SlotFlags.NECK |
        SlotFlags.INNERCLOTHING |
        SlotFlags.OUTERCLOTHING;
    [Dependency] protected MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] protected SharedWieldableSystem _wield = default!;
    public void InitializeBuckle()
    {
        SubscribeLocalEvent<VehicleBuckleComponent, StrappedEvent>(OnStrapped, after: [typeof(MovementSpeedModifierSystem)]);
        SubscribeLocalEvent<VehicleBuckleComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<VehicleBuckleComponent, UnstrapAttemptEvent>(OnUnstrapAttempt);
        SubscribeLocalEvent<VehicleBuckleComponent, VehicleUnbuckleDoAfter>(OnUnbuckleDoAfter);
        SubscribeLocalEvent<VehicleBuckleComponent, RefreshMovementSpeedModifiersEvent>(OnMovementSpeedRefreshVehicleEvent, after: [typeof(MovementSpeedModifierSystem)]);
    }

    private void OnStrapped(Entity<VehicleBuckleComponent> ent, ref StrappedEvent args)
    {
        SetUpRider(args.Buckle.Owner, ent.Owner);
        foreach(var held in _handsSystem.EnumerateHeld(args.Buckle.Owner))
        {
            if(TryComp<WieldableComponent>(held, out var wieldComp))
                _wield.TryUnwield(held, wieldComp, args.Buckle.Owner);
        }
        _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnUnstrapAttempt(Entity<VehicleBuckleComponent> ent, ref UnstrapAttemptEvent args)
    {
        if(!TryComp<VehicleComponent>(ent.Owner, out var vehicleComp)) return;
        if(args.User == null || !args.Popup) return;
        if(vehicleComp.Rider == null) return;
        if (vehicleComp.Rider != args.User)
        {
            args.Cancelled = true;
            _adminLogger.Add(Database.LogType.Action, Database.LogImpact.Low, $"{ToPrettyString(args.User)} attempted to steal vehicle {ToPrettyString(ent.Owner)}");
            _popup.PopupClient(Loc.GetString("vehicle-steal-vehicle-attempt"), vehicleComp.Rider.Value, PopupType.LargeCaution);
            var ev = new VehicleUnbuckleDoAfter();
            var doAfter = new DoAfterArgs(EntityManager, args.User.Value, ent.Comp.duration, ev, ent.Owner, ent.Owner)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            };
            _doAfter.TryStartDoAfter(doAfter);
        }
    }
    
    private void OnHornActionEvent(Entity<VehicleComponent> ent, ref HornActionEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted) return;
        if (args.Handled || ent.Comp.HornSound == null)
            return;
        if(ent.Comp.Rider == null) return;
        _audio.PlayPredicted(ent.Comp.HornSound, ent.Owner, null);
        args.Handled = true;
    }

    private void OnUnstrapped(Entity<VehicleBuckleComponent> ent, ref UnstrappedEvent args)
    {                
        if(HasComp<RiderComponent>(args.Buckle.Owner))
            RemoveRider(args.Buckle.Owner, ent.Owner);
    }

    private void OnUnbuckleDoAfter(Entity<VehicleBuckleComponent> ent, ref VehicleUnbuckleDoAfter args)
    {
        if(args.Cancelled) return;
        if(!TryComp<VehicleComponent>(ent.Owner, out var vehicleComp)) return;
        if(vehicleComp.Rider == null) return;
        var user = vehicleComp.Rider.Value;
        if(!TryComp<BuckleComponent>(user, out var buckleComp)) return;
        _buckle.Unbuckle((user, buckleComp), user);
    }

    private void OnMovementSpeedRefreshVehicleEvent(Entity<VehicleBuckleComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if(!ent.Comp.armoraffectsvehicle) return;
        if(!TryComp<VehicleComponent>(ent.Owner, out var vehicleComp) || vehicleComp.Rider == null) return;
         
        var clothingSpeedEv = new ClothingSpeedModifierQueryEvent(WeightSlots);
        RaiseLocalEvent(vehicleComp.Rider.Value, clothingSpeedEv);
        args.ModifySpeed(clothingSpeedEv.TotalWalkCoefficient, clothingSpeedEv.TotalSprintCoefficient);
    }
}