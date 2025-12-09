using Content.Shared._FarHorizons.Vehicles.EntitySystems;
using Content.Shared._FarHorizons.VehicleBuckle.Components;
using Content.Shared._FarHorizons.Vehicles.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Access.Components;
using Content.Shared.DoAfter;
using Content.Shared._FarHorizons.Vehicles;
using System.Numerics;
using Content.Shared.Buckle;
using Content.Shared.Popups;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._FarHorizons.Vehicle;

public sealed class VehicleSystems : SharedVehicleSystems
{    
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleComponent, MapInitEvent>(OnModMapInit);
        SubscribeLocalEvent<VehicleComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);
        SubscribeLocalEvent<VehicleBuckleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleBuckleComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<VehicleBuckleComponent, UnstrapAttemptEvent>(OnUnstrapAttempt);
        SubscribeLocalEvent<VehicleBuckleComponent, VehicleUnbuckleDoAfter>(OnUnbuckleDoAfter);
        SubscribeLocalEvent<RiderComponent, PullAttemptEvent>(OnPullAttempt);
        _transform.OnGlobalMoveEvent += OnMoveEvent;
    }

    private void OnModMapInit(Entity<VehicleComponent> ent, ref MapInitEvent args)
    {
        if(!TryComp<MovementSpeedModifierComponent>(ent.Owner, out var msmComp)) return;
        _movementSpeed.ChangeFrictionAndAcceleration(ent.Owner, ent.Comp.Friction, ent.Comp.FrictionNoInput, ent.Comp.Acceleration, msmComp);
    }

    private void OnStrapped(Entity<VehicleBuckleComponent> ent, ref StrappedEvent args)
    {
        if(!TryComp<VehicleComponent>(ent, out var vehicleComp)) return;
        if(vehicleComp.Rider == null)
        {
            EnsureComp<RiderComponent>(args.Buckle.Owner);
            Comp<RiderComponent>(args.Buckle.Owner).Riding = ent.Owner;

            _mover.SetRelay(args.Buckle.Owner, ent.Owner);
            vehicleComp.Rider = args.Buckle.Owner;

            if(TryComp<InputMoverComponent>(ent.Owner, out var mover))
            {
                mover.CanMove = true;
                Dirty(ent.Owner, mover);
            }
        }
    }
    
    private void OnUnstrapped(Entity<VehicleBuckleComponent> ent, ref UnstrappedEvent args)
    {
        if(!TryComp<VehicleComponent>(ent, out var vehicleComp)) return;

        if(HasComp<RelayInputMoverComponent>(args.Buckle.Owner))
            RemComp<RelayInputMoverComponent>(args.Buckle.Owner);
        if(HasComp<RiderComponent>(args.Buckle.Owner))
            RemComp<RiderComponent>(args.Buckle.Owner);
        vehicleComp.Rider = null;
    }

    private void OnUnstrapAttempt(Entity<VehicleBuckleComponent> ent, ref UnstrapAttemptEvent args)
    {
        if(!TryComp<VehicleComponent>(ent.Owner, out var vehicleComp)) return;
        if(args.User == null) return;
        if(vehicleComp.Rider == null) return;
        if (vehicleComp.Rider != args.User)
        {
            args.Cancelled = true;
            _popup.PopupEntity($"Someone starts to remove you from the driver seat.", vehicleComp.Rider.Value, PopupType.LargeCaution);
            var ev = new VehicleUnbuckleDoAfter();
            var doAfter = new DoAfterArgs(EntityManager, args.User.Value, ent.Comp.duration, ev, ent.Owner)
            {
                BreakOnMove = true
            };
            _doAfter.TryStartDoAfter(doAfter);
        }
    }

    private void OnGetAdditionalAccess(Entity<VehicleComponent> ent, ref GetAdditionalAccessEvent args)
    {
        if (ent.Comp.Rider == null) return;

        args.Entities.Add(ent.Comp.Rider.Value);
    }

    private void OnPullAttempt(Entity<RiderComponent> ent, ref PullAttemptEvent args)
    {
        if (ent.Owner != args.PullerUid)
            args.Cancelled = true;
    }
    
    private void OnMoveEvent(ref MoveEvent ev)
    {
        var rider = ev.Entity.Owner; 
        if(!TryComp<RiderComponent>(rider, out var ridercomp)) return;
        if(!TryComp<PhysicsComponent>(rider, out var riderPhys)) return;
        var vehicle = ridercomp.Riding;
        if(!TryComp<PhysicsComponent>(vehicle, out var vehiclePhys)) return;
        var riderTransform = Transform(rider);
        if(riderTransform.ParentUid !=  vehicle) return;
        
        if(riderTransform.LocalPosition.X != 0 || riderTransform.LocalPosition.Y != 0)
            _transform.SetLocalPosition(rider, new Vector2(0f, 0f), riderTransform);
        if(riderTransform.LocalRotation != 0)
            _transform.SetLocalRotation(rider, 0f, riderTransform);
        
        if(!TryComp<MovementSpeedModifierComponent>(vehicle, out var moveComp)) return;
        if(Math.Abs(vehiclePhys.LinearVelocity.X) > 1.05*moveComp.BaseSprintSpeed || 
            Math.Abs(vehiclePhys.LinearVelocity.Y) > 1.05*moveComp.BaseSprintSpeed)
        {
            if(TryComp<BuckleComponent>(rider, out var buckleComp))
                if(_buckle.TryUnbuckle(rider, null, buckleComp))
                {
                    _physics.ApplyLinearImpulse(rider, vehiclePhys.LinearVelocity, body: riderPhys);
                }
        }
    }

    private void OnUnbuckleDoAfter(Entity<VehicleBuckleComponent> ent, ref VehicleUnbuckleDoAfter args)
    {
        if(!TryComp<VehicleComponent>(ent.Owner, out var vehicleComp)) return;
        if(vehicleComp.Rider == null) return;
        var user = vehicleComp.Rider.Value;
        if(!TryComp<BuckleComponent>(user, out var buckleComp)) return;
        _buckle.Unbuckle((user, buckleComp), user);
    }
}