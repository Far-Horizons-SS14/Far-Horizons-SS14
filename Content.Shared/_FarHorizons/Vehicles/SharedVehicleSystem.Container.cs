using Robust.Shared.Containers;
using Content.Shared.Verbs;
using Content.Shared.DoAfter;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.DragDrop;

namespace Content.Shared._FarHorizons.Vehicles;

public abstract partial class SharedVehicleSystem
{    
    [SubscribeLocalEvent]
    private void OnVehicleEntryDoAfter(Entity<VehicleContainerComponent> ent, ref VehicleEntryDoAfter args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if(!TryInsert(args.Args.Target, ent.Owner)) return;

        SetUpRider(args.Args.Target!.Value, ent.Owner);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnVehicleRemoveDoAfter(Entity<VehicleContainerComponent> ent, ref VehicleRemoveDoAfter args)
    {
        if (args.Cancelled || args.Handled)
            return;
        
        var target = GetEntity(args.Passenger);
        RemoveRider(target, ent.Owner);
        TryRemove(target, ent.Owner);

        args.Handled = true;
    }

    [SubscribeLocalEvent(after:[typeof(SharedContainerSystem)])]
    private void OnInsertAttempt(Entity<VehicleContainerComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (ent.Comp.PassengerSlot == null || args.Container.ID != ent.Comp.PassengerSlot.ID || _tags.HasTag(args.EntityUid, s_vehicleKeyTag)) return;
        if (_whitelist.IsWhitelistFail(ent.Comp.PassengerWhitelist, args.EntityUid))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnAlternativeVerb(Entity<VehicleContainerComponent> ent , ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if(!TryComp<VehicleComponent>(ent.Owner, out var vehicleComp) || vehicleComp.isBroken) return; 
        var user = args.User;

        if (CanInsert(ent.Owner) && !vehicleComp.Passengers.Contains(user))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("vehicle-verb-enter"),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, user, ent.Comp.EntryTime, new VehicleEntryDoAfter(), ent.Owner, target: user)
                    {
                        BreakOnMove = true,
                    };
                        
                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(enterVerb);
        }
        else if(vehicleComp.Passengers.Contains(user))
        {
            var exitVerb = new AlternativeVerb
            {
                Text = Loc.GetString("vehicle-verb-leave"),
                Act = () =>
                {
                    TryRemove(user, ent.Owner);
                    if(HasComp<RiderComponent>(user))
                        RemoveRider(user, ent.Owner);
                }
            };
            args.Verbs.Add(exitVerb);
        }
            
        if(vehicleComp.Passengers.Count != 0 && !vehicleComp.Passengers.Contains(user))
        {
            var category = new VerbCategory("Remove", null);
            foreach (var passenger in vehicleComp.Passengers)
            {
                var removeVerb = new AlternativeVerb
                {
                    Text = Loc.GetString("vehicle-verb-remove", ("passenger", MetaData(passenger).EntityName)),
                    Category = category,
                    Act = () =>
                    {
                        if(_gameTiming.IsFirstTimePredicted && _net.IsClient)
                            _popup.PopupClient(Loc.GetString("vehicle-remove-passenger-attempt", ("user", MetaData(user).EntityName), ("passenger", MetaData(passenger).EntityName)), ent.Owner, passenger, PopupType.LargeCaution);
                            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, ent.Comp.RemoveTime, new VehicleRemoveDoAfter(GetNetEntity(passenger)), ent.Owner, target: ent.Owner)
                        {
                            BreakOnMove = true,
                        };
                        _adminLogger.Add(LogType.Verb, LogImpact.Medium, $"{ToPrettyString(user)} attempted to remove a passenger from {ToPrettyString(ent.Owner)}");

                        _doAfter.TryStartDoAfter(doAfterEventArgs);
                    }
                };
                args.Verbs.Add(removeVerb);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnDragDrop(Entity<VehicleContainerComponent> ent, ref DragDropTargetEvent args)
    {
        if(args.Handled) return;
        args.Handled = true;

        if(!CanInsert(ent.Owner)) return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.EntryTime, new VehicleEntryDoAfter(), ent.Owner, target: args.Dragged)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private bool TryInsert(EntityUid? rider, Entity<VehicleContainerComponent?> vehicle)
    {
        if(!Resolve(vehicle.Owner, ref vehicle.Comp))
            return false;

        if(rider == null)
            return false;
                
        if (!CanInsert(vehicle))
            return false;

        _container.Insert(rider.Value, vehicle.Comp.PassengerSlot);
        Dirty(vehicle);
        return true;
    }

    public bool CanInsert(Entity<VehicleContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if(!TryComp<VehicleComponent>(ent.Owner, out var vehicleComp) || vehicleComp.isBroken)
            return false;

        return vehicleComp.Passengers.Count < ent.Comp.Seats;
    }

    public bool TryRemove(EntityUid? rider, Entity<VehicleContainerComponent?> vehicle)
    {
        if(!Resolve(vehicle, ref vehicle.Comp))
            return false;

        if(rider == null)
            return false;

        _container.Remove(rider.Value, vehicle.Comp.PassengerSlot);
        Dirty(vehicle);
        return true;
    }
}