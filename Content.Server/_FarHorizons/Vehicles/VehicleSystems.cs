using Content.Shared._FarHorizons.Vehicles.EntitySystems;
using Content.Shared._FarHorizons.VehicleBuckle.Components;
using Content.Shared._FarHorizons.Vehicles.Components;
using Content.Shared._Starlight.Actions.Events;
using Content.Shared.Access.Components;
using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling.Events;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using System.Numerics;
using System.Linq;
using Content.Server.PowerCell;
using Content.Shared.PowerCell;
using Robust.Server.GameObjects;
using Content.Shared._FarHorizons.Vehicles;
using Robust.Shared.Physics.Events;
using Content.Server.Stunnable;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;

namespace Content.Server._FarHorizons.Vehicle;

public sealed partial class VehicleSystems : SharedVehicleSystems
{    
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private static readonly ProtoId<TagPrototype> _vehicleKeyTag = "VehicleKey";
    private EntityQuery<ProjectileComponent> _projQuery;
    public override void Initialize()
    {
        base.Initialize();

        _projQuery = GetEntityQuery<ProjectileComponent>();

        SubscribeLocalEvent<VehicleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VehicleComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);
        SubscribeLocalEvent<VehicleComponent, ItemSlotInsertEvent>(OnInsertEvent);
        SubscribeLocalEvent<VehicleComponent, StartCollideEvent>(HandleCollide);

        SubscribeLocalEvent<VehicleBuckleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleBuckleComponent, UnstrappedEvent>(OnUnstrapped);

        SubscribeLocalEvent<RiderComponent, BeingPulledAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<RiderComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<RiderComponent, KnockedDownEvent>(OnKnockdown);
        SubscribeLocalEvent<RiderComponent, UpdateCanMoveEvent>(OnUpdateCanMoveEvent);
        SubscribeLocalEvent<RiderComponent, JumpActionEvent>(OnJumpActionEvent);

        SubscribeLocalEvent<TransformComponent, JetJumpActionEvent>(OnJetJumpActionEvent);

        _transform.OnGlobalMoveEvent += OnMoveEvent;
    }

    private void OnMapInit(Entity<VehicleComponent> ent, ref MapInitEvent args)
    {
        if(!TryComp<MovementSpeedModifierComponent>(ent.Owner, out var msmComp)) return;
        _movementSpeed.ChangeFrictionAndAcceleration(ent.Owner, ent.Comp.Friction, ent.Comp.FrictionNoInput, ent.Comp.Acceleration, msmComp);
        _appearance.SetData(ent.Owner, VehicleVisuals.AutoAnimate, false);
    }

    private void OnInsertEvent(Entity<VehicleComponent> ent, ref ItemSlotInsertEvent args)
    {
        if(ent.Comp.Rider == null) return;
        if(_tags.HasTag(args.Item, _vehicleKeyTag))
        {
            AddActions(ent.Comp.Rider.Value, ent.Owner, ent.Comp);
        }
    }

    private void HandleCollide(Entity<VehicleComponent> ent, ref StartCollideEvent args)
    {
        if(ent.Comp.Rider == null) return;
        var rider = ent.Comp.Rider.Value;
        
        if(TryComp<VehicleBuckleComponent>(ent.Owner, out var vehicleBuckleComp))
        {
            if(!vehicleBuckleComp.ejectOnCrash) return;

            if (!args.OurFixture.Hard || !args.OtherFixture.Hard) return;

            var speed = args.OurBody.LinearVelocity.Length();

            if (speed < vehicleBuckleComp.SpeedToEjectOnCrash) return;
            
            if(!TryComp<PhysicsComponent>(ent.Owner, out var vehiclePhys) || !TryComp<PhysicsComponent>(rider, out var riderPhys))
                return;

            if(TryComp<BuckleComponent>(rider, out var buckleComp))
                if(_buckle.TryUnbuckle(rider, null, buckleComp))
                {
                    var riderXform = Transform(rider);
                    _stun.TryCrawling(rider, TimeSpan.FromSeconds(3));
                    _throwing.TryThrow(rider, vehiclePhys.LinearVelocity, riderPhys, riderXform, _projQuery, vehiclePhys.LinearVelocity.Length(), playSound: false);
                }
        }
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
            _actionBlocker.UpdateCanMove(args.Buckle.Owner);
            if (vehicleComp.requireIgnition
                && TryComp(ent.Owner, out ItemSlotsComponent? itemComp)
                && !itemComp.Slots.Values.Any(slot =>
                    slot.ContainerSlot?.ContainedEntity is EntityUid item
                    && _tags.HasTag(item, _vehicleKeyTag))) return;
            AddActions(vehicleComp.Rider.Value, ent.Owner, vehicleComp);
            Dirty(ent.Owner, ent.Comp);
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
        _actionBlocker.UpdateCanMove(args.Buckle.Owner);
        _actions.RemoveProvidedActions(args.Buckle.Owner, ent.Owner);
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnGetAdditionalAccess(Entity<VehicleComponent> ent, ref GetAdditionalAccessEvent args)
    {
        if (ent.Comp.Rider == null) return;

        args.Entities.Add(ent.Comp.Rider.Value);
    }

    private void OnPullAttempt(Entity<RiderComponent> ent, ref BeingPulledAttemptEvent args)
    {
        if (ent.Owner != args.Puller)
            args.Cancel();
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

    private void OnJetJumpActionEvent(Entity<TransformComponent> ent, ref JetJumpActionEvent args)
    {
        if(!TryComp<BuckleComponent>(ent.Comp.ParentUid, out var buckleComp)) return;
        _buckle.Unbuckle((ent.Comp.ParentUid, buckleComp), ent.Comp.ParentUid);
    }

    private void OnMoveEvent(ref MoveEvent ev)
    {
        var vehicle = ev.Entity.Owner; 
        if(!TryComp<VehicleComponent>(vehicle, out var vehicleComp)) return;
        if(!TryComp<PhysicsComponent>(vehicle, out var vehiclePhys)) return;

        if(Math.Abs(vehiclePhys.LinearVelocity.X) > 0.3 || Math.Abs(vehiclePhys.LinearVelocity.Y) > 0.3)
            _appearance.SetData(vehicle, VehicleVisuals.AutoAnimate, true);
        if(Math.Abs(vehiclePhys.LinearVelocity.X) < 0.3 && Math.Abs(vehiclePhys.LinearVelocity.Y) < 0.3)
            _appearance.SetData(vehicle, VehicleVisuals.AutoAnimate, false);

        if( vehicleComp.Rider == null) return;
        var rider = vehicleComp.Rider.Value;
        if(!TryComp<PhysicsComponent>(rider, out var riderPhys)) return;
        var riderTransform = Transform(rider);
        if(riderTransform.ParentUid !=  vehicle) return;

        if(HasComp<VehicleBuckleComponent>(vehicle) && TryComp<StrapComponent>(vehicle, out var strapComp))
        {
            if(riderTransform.LocalPosition.X != 0+strapComp.BuckleOffset.X || riderTransform.LocalPosition.Y != 0+strapComp.BuckleOffset.Y)
                _transform.SetLocalPosition(rider, new Vector2(0f+strapComp.BuckleOffset.X, 0f+strapComp.BuckleOffset.Y), riderTransform);
            if(riderTransform.LocalRotation != 0)
                _transform.SetLocalRotation(rider, 0f, riderTransform);
        }
        
        if(!TryComp<MovementSpeedModifierComponent>(vehicle, out var moveComp)) return;
        if(Math.Abs(vehiclePhys.LinearVelocity.X) > 1.05*moveComp.CurrentSprintSpeed || 
            Math.Abs(vehiclePhys.LinearVelocity.Y) > 1.05*moveComp.CurrentSprintSpeed)
        {
            if(TryComp<BuckleComponent>(rider, out var buckleComp))
                if(_buckle.TryUnbuckle(rider, null, buckleComp))
                {
                    _stun.TryCrawling(rider, TimeSpan.FromSeconds(3));
                    _throwing.TryThrow(rider, vehiclePhys.LinearVelocity*riderPhys.Mass, riderPhys, riderTransform, _projQuery, vehiclePhys.LinearVelocity.Length(), playSound: false);
                }
        }

        if(!_powerCell.HasDrawCharge(vehicle))
            _actionBlocker.UpdateCanMove(rider);
    }

    private void OnUpdateCanMoveEvent(Entity<RiderComponent> ent, ref UpdateCanMoveEvent args)
    {
        if(!TryComp<VehicleComponent>(ent.Comp.Riding, out var vehicleComp)) return;
        if(vehicleComp.requireIgnition && !vehicleComp.Started)
        {
            args.Cancel();
        }
        if(ent.Comp.Riding != null && !_powerCell.HasDrawCharge(ent.Comp.Riding.Value))
        {
            if(TryComp<PowerCellDrawComponent>(ent.Comp.Riding.Value, out var pcdComp) && pcdComp.Enabled)
                pcdComp.Enabled = false;
            if(vehicleComp.Started)
                vehicleComp.Started = false;
            args.Cancel();
        }
        Dirty(ent.Owner, vehicleComp);
    }

    private void AddActions(EntityUid rider, EntityUid vehicle, VehicleComponent? component=null)
    {
        if (!Resolve(vehicle, ref component))
            return;
        if(component.requireIgnition)
            _actions.AddAction(rider, ref component.TurnKeysActionEntity, component.TurnKeysAction, vehicle);
    }
}