using Content.Shared._FarHorizons.Vehicles;
using Content.Server.Destructible;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Audio;
using Content.Shared.Buckle.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Movement.Systems;
using Content.Shared.Repairable;
using Content.Shared.Database;
using Content.Shared.Throwing;

namespace Content.Server._FarHorizons.Vehicles;

public sealed partial class VehicleSystem : SharedVehicleSystem
{    
    [Dependency] private MovementModStatusSystem _movementStatus = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private DestructibleSystem _destructible = default!;
    private static readonly string _bluntname = "Blunt";

    public override void Initialize()
    {
        base.Initialize();
        InitializeEquipment();
        InitializeAtmos();

        SubscribeLocalEvent<VehicleComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<VehicleComponent, RepairedEvent>(OnRepairFinished);
    }

    protected override void OnComponentStartup(Entity<VehicleComponent> ent, ref ComponentStartup args)
    {
        base.OnComponentStartup(ent, ref args);
        ent.Comp.MaxIntegrity = _destructible.DestroyedAt(ent);
        Dirty(ent);
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
                        _throwing.TryThrow(rider, vehiclePhys.LinearVelocity, riderPhys, riderXform, vehiclePhys.LinearVelocity.Length(), playSound: false);
                        _adminLogger.Add(LogType.Slip, LogImpact.Medium, $"{ToPrettyString(rider)} was launched from vehicle {ToPrettyString(ent.Owner)}");
                    }
            }
            else
            {
                foreach(var passenger in ent.Comp.Passengers)
                {
                    _stun.TryAddStunDuration(passenger, TimeSpan.FromSeconds(3));
                }
            }   
        }
        else if(args.OurFixture.Hard && !args.OtherFixture.Hard)
        {
            if(!HasComp<DamageableComponent>(args.OtherEntity) || HasComp<PacifiedComponent>(ent.Owner)) return; 

            _audio.PlayPredicted(ent.Comp.SoundHit, ent.Owner, null, AudioParams.Default.WithVariation(0.125f).WithVolume(-0.125f));

            DamageTypePrototype? _blunt = _prototypes.Index<DamageTypePrototype>(_bluntname);
            DamageSpecifier? _damage = new(_blunt, Math.Clamp(10 * (1 + (0.5 * speed / crashingSpeed)), 10, 20));
            _damageable.TryChangeDamage(args.OtherEntity, _damage, origin: ent.Comp.Rider.Value);
            
            _movementStatus.TryAddMovementSpeedModDuration(ent.Owner, "StatusEffectSlowdownNoMobState", TimeSpan.FromSeconds(2), 0.25f);
            _adminLogger.Add(LogType.Damaged, LogImpact.High, $"{ToPrettyString(ent.Comp.Rider.Value)} ran over {ToPrettyString(args.OtherEntity)} dealing {_damage}");
        }
    }

    private void OnRepairFinished(Entity<VehicleComponent> ent, ref RepairedEvent args)
    {
        _adminLogger.Add(LogType.Healed, LogImpact.Low, $"{ToPrettyString(args.User)} repaired the vehicle {ToPrettyString(ent.Owner)}");
        ent.Comp.isBroken = false;
        
        if(HasComp<VehicleBuckleComponent>(ent))
        {
            _buckle.StrapSetEnabled(ent, true);
        }
        TryUpdateVisualState(ent.Owner);
        Dirty(ent.Owner, ent.Comp);
    }
}
