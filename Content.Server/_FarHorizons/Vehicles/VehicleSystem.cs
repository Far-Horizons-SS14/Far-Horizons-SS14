using Content.Shared._FarHorizons.Vehicles.Components;
using System.Linq;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Verbs;
using Content.Server.Destructible;
using Content.Shared._FarHorizons.Vehicles.Events;
using Content.Shared._FarHorizons.Vehicles;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Audio;
using Content.Shared.Buckle.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Player;
using Content.Shared.Damage;
using Content.Shared.Movement.Systems;
using Content.Shared.Repairable;
using Content.Shared.Database;

namespace Content.Server._FarHorizons.Vehicles;

public sealed partial class VehicleSystems : SharedVehicleSystem
{    
    [Dependency] private readonly MovementModStatusSystem _movementStatus = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleContainerComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<VehicleContainerComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<VehicleComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<VehicleComponent, RepairedEvent>(OnRepairFinished);
    }

    private void HandleCollide(Entity<VehicleComponent> ent, ref StartCollideEvent args)
    {
        if(ent.Comp.Rider == null) return;
        var rider = ent.Comp.Rider.Value;
        
        if(!ent.Comp.AllowCrashing) return;
        if(!TryComp<MovementSpeedModifierComponent>(ent.Owner, out var msmComp)) return; 

        var speed = args.OurBody.LinearVelocity.Length();
        var crashingSpeed = 0f;

        if(msmComp.BaseSprintSpeed > msmComp.BaseWalkSpeed)
            crashingSpeed = msmComp.BaseWalkSpeed+1;
        else if(msmComp.BaseSprintSpeed < msmComp.BaseWalkSpeed)
            crashingSpeed = msmComp.BaseSprintSpeed+1;
        
        if(crashingSpeed < 8f)
            crashingSpeed = 8f;
            
        if (speed < crashingSpeed) return;
        
        if (args.OurFixture.Hard && args.OtherFixture.Hard)
        {
            _audio.PlayPredicted(ent.Comp.SoundHit, ent.Owner, null, AudioParams.Default.WithVariation(0.125f).WithVolume(-0.125f));
                
            if(TryComp<VehicleBuckleComponent>(ent.Owner, out var vbComp) && TryComp<BuckleComponent>(rider, out var buckleComp))
            {
                if(TryComp<PhysicsComponent>(ent.Owner, out var vehiclePhys) && TryComp<PhysicsComponent>(rider, out var riderPhys))
                    if(_buckle.TryUnbuckle(rider, null, buckleComp) && vbComp.EjectOnCrash)
                    {
                        var riderXform = Transform(rider);
                        _stun.TryCrawling(rider, TimeSpan.FromSeconds(3));
                        _throwing.TryThrow(rider, vehiclePhys.LinearVelocity, riderPhys, riderXform, _projQuery, vehiclePhys.LinearVelocity.Length(), playSound: false);
                    _adminLogger.Add(LogType.Slip, LogImpact.Medium, $"{ToPrettyString(rider)} was launched from vehicle {ToPrettyString(ent.Owner)}");
                    }
            }
            else if(TryComp<VehicleContainerComponent>(ent.Owner, out var vcComp))
            {
                foreach(var passenger in vcComp.PassengerSlot.ContainedEntities)
                {
                    _stun.TryAddStunDuration(passenger, TimeSpan.FromSeconds(3));
                }
            }   
        }
        else if(args.OurFixture.Hard && !args.OtherFixture.Hard)
        {
            if(!HasComp<DamageableComponent>(args.OtherEntity) || HasComp<PacifiedComponent>(ent.Owner)) return; 

            _audio.PlayPredicted(ent.Comp.SoundHit, ent.Owner, null, AudioParams.Default.WithVariation(0.125f).WithVolume(-0.125f));

            DamageTypePrototype? _blunt = _prototypes.Index<DamageTypePrototype>(s_bluntname);
            DamageSpecifier? _damage = new(_blunt, Math.Clamp(10 * (1 + (0.5 * speed / crashingSpeed)), 10, 20));
            _damageable.TryChangeDamage(args.OtherEntity, _damage, origin: ent.Comp.Rider.Value);
            _color.RaiseEffect(Color.Red, new List<EntityUid>() { args.OtherEntity, }, Filter.Pvs(args.OtherEntity, entityManager: EntityManager));

            _movementStatus.TryAddMovementSpeedModDuration(ent.Owner, "StatusEffectSlowdownNoMobState", TimeSpan.FromSeconds(2), 0.25f);
            _adminLogger.Add(LogType.Damaged, LogImpact.High, $"{ToPrettyString(ent.Comp.Rider.Value)} ran over {ToPrettyString(args.OtherEntity)} dealing {_damage}");
        }
    }

    private void OnRepairFinished(Entity<VehicleComponent> ent, ref RepairedEvent args)
    {
        _adminLogger.Add(LogType.Healed, LogImpact.Low, $"{ToPrettyString(args.User)} repaired the vehicle {ToPrettyString(ent.Owner)}");
        ent.Comp.isBroken = false;
        
        if(TryComp<VehicleBuckleComponent>(ent, out var vbComp))
        {
            _buckle.StrapSetEnabled(ent, true);
        }
        TryUpdateVisualState(ent.Owner);
        Dirty(ent.Owner, ent.Comp);
    }
    private void OnAlternativeVerb(EntityUid uid, VehicleContainerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;
        if(!TryComp<VehicleComponent>(uid, out var vehicleComp)) return; 
        if(TryComp<DestructibleComponent>(uid, out var destructibleComp) && destructibleComp.IsBroken) return;

        if (CanInsert(uid, component) && !component.PassengerSlot.ContainedEntities.Contains(args.User))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("vehicle-verb-enter"),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.EntryTime, new VehicleEntryDoAfter(), uid, target: args.User)
                    {
                        BreakOnMove = true,
                    };
                    
                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(enterVerb);
        }
        else if(component.PassengerSlot.ContainedEntities.Contains(args.User))
        {
            var exitVerb = new AlternativeVerb
            {
                Text = Loc.GetString("vehicle-verb-leave"),
                Act = () =>
                {
                    TryRemove(args.User, uid, component);
                    if(HasComp<RiderComponent>(args.User))
                        RemoveRider(args.User, uid, vehicleComp);
                }
            };
            args.Verbs.Add(exitVerb);
        }
        
        if(component.PassengerSlot.ContainedEntities.Count != 0 && !component.PassengerSlot.ContainedEntities.Contains(args.User))
        {
            var removeVerb = new AlternativeVerb
            {
                Text = Loc.GetString("vehicle-verb-remove"),
                Act = () =>
                {
                    _popup.PopupEntity(Loc.GetString("vehicle-remove-passenger-attempt"), uid, PopupType.LargeCaution);
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.RemoveTime, new VehicleRemoveDoAfter(), uid, target: uid)
                    {
                        BreakOnMove = true,
                    };
                    _adminLogger.Add(Shared.Database.LogType.Verb, Shared.Database.LogImpact.Medium, $"{ToPrettyString(args.User)} attempted to remove a passenger from {ToPrettyString(uid)}");

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(removeVerb);
        }
    }
    
    private void OnDragDrop(Entity<VehicleContainerComponent> ent, ref DragDropTargetEvent args)
    {
        if(args.Handled) return;
        args.Handled = true;
        if(TryComp<DestructibleComponent>(ent.Owner, out var destructibleComp) && destructibleComp.IsBroken) return;

        if(!CanInsert(ent.Owner, ent.Comp)) return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.EntryTime, new VehicleEntryDoAfter(), ent.Owner, target: args.Dragged)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }
}