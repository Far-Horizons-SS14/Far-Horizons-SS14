using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared._FarHorizons.VehicleBuckle.Components;
using Content.Shared._FarHorizons.Vehicles.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using System.Linq;

namespace Content.Shared._FarHorizons.Vehicles.EntitySystems;

public abstract partial class SharedVehicleSystems : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    private static readonly ProtoId<TagPrototype> _vehicleKeyTag = "VehicleKey";

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleBuckleComponent, UnstrapAttemptEvent>(OnUnstrapAttempt);
        SubscribeLocalEvent<VehicleBuckleComponent, VehicleUnbuckleDoAfter>(OnUnbuckleDoAfter);

        SubscribeLocalEvent<VehicleComponent, TurnKeysEvent>(OnTurnKeysEvent);
        SubscribeLocalEvent<VehicleComponent, TurnKeysDoAfter>(OnTurnKeysDoAfter);
        SubscribeLocalEvent<VehicleComponent, ItemSlotEjectEvent>(OnEjectEvent);
        SubscribeLocalEvent<VehicleComponent, EjectKeysDoAfter>(OnEjectKeysDoAfter);
    }

    private void OnUnstrapAttempt(Entity<VehicleBuckleComponent> ent, ref UnstrapAttemptEvent args)
    {
        if(!TryComp<VehicleComponent>(ent.Owner, out var vehicleComp)) return;
        if(args.User == null || !args.Popup) return;
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
    private void OnUnbuckleDoAfter(Entity<VehicleBuckleComponent> ent, ref VehicleUnbuckleDoAfter args)
    {
        if(args.Cancelled) return;
        if(!TryComp<VehicleComponent>(ent.Owner, out var vehicleComp)) return;
        if(vehicleComp.Rider == null) return;
        var user = vehicleComp.Rider.Value;
        if(!TryComp<BuckleComponent>(user, out var buckleComp)) return;
        _buckle.Unbuckle((user, buckleComp), user);
    }

    private void OnTurnKeysEvent(Entity<VehicleComponent> ent, ref TurnKeysEvent args)
    {
        if(ent.Comp.Rider == null) return;
        _popup.PopupEntity($"You turn the key for the vehicle.", ent.Owner, PopupType.Medium);
        var ev = new TurnKeysDoAfter();
        var doAfter = new DoAfterArgs(EntityManager, ent.Comp.Rider.Value, ent.Comp.startupTime, ev, ent.Owner)
        {
            BreakOnMove = true
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnTurnKeysDoAfter(Entity<VehicleComponent> ent, ref TurnKeysDoAfter args)
    {
        if(args.Cancelled) return;
        ent.Comp.Started = !ent.Comp.Started;
        if(ent.Comp.Rider != null)
            _actionBlocker.UpdateCanMove(ent.Comp.Rider.Value);
    }

    private void OnEjectEvent(Entity<VehicleComponent> ent, ref ItemSlotEjectEvent args)
    {
        if(ent.Comp.Rider == null) return;
        if(args.User == null) return;
        if(_tags.HasTag(args.Item, _vehicleKeyTag))
        {
            if(ent.Comp.Rider == args.User)
            {
                if(ent.Comp.Started)
                    ent.Comp.Started = false;
                _actionBlocker.UpdateCanMove(ent.Comp.Rider.Value);
                _actions.RemoveProvidedActions(ent.Comp.Rider.Value, ent.Owner);
            }
            else
            {
                args.Cancelled = true;
                _popup.PopupEntity($"Someone is trying to steal the keys from the ignition.", ent.Comp.Rider.Value, PopupType.LargeCaution);
                var ev = new EjectKeysDoAfter();
                var doAfter = new DoAfterArgs(EntityManager, args.User.Value, ent.Comp.timeToStealKeys, ev, ent.Owner)
                {
                    BreakOnMove = true,
                    CancelDuplicate = false

                };
                _doAfter.TryStartDoAfter(doAfter);
            }
        }
    }
    
    private void OnEjectKeysDoAfter(Entity<VehicleComponent> ent, ref EjectKeysDoAfter args)
    {
        if(args.Cancelled) return;
        if(TryComp<ContainerManagerComponent>(ent.Owner, out var container))
        {
            var keys = container.Containers.Values.FirstOrDefault(x => _tags.HasTag(x.ContainedEntities.First(), _vehicleKeyTag))!.ContainedEntities.First();
            _handsSystem.PickupOrDrop(args.User, keys);
            if(ent.Comp.Started)
                ent.Comp.Started = false;
            if(ent.Comp.Rider == null) return;
            _actionBlocker.UpdateCanMove(ent.Comp.Rider.Value);
            _actions.RemoveProvidedActions(ent.Comp.Rider.Value, ent.Owner);
        }
    }
}