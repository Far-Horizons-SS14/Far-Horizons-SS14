using Content.Shared._FarHorizons.Vehicle.EntitySystems;
using Content.Shared._FarHorizons.Vehicle.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server._FarHorizons.Vehicle;

public sealed class VehicleSystems : SharedVehicleSystems
{    
    [Dependency] private readonly SharedMoverController _mover = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args)
    {
        if(ent.Comp.Rider == null)
        {
            _mover.SetRelay(args.Buckle.Owner, ent.Owner);
            ent.Comp.Rider = args.Buckle.Owner;

            if(TryComp<InputMoverComponent>(ent.Owner, out var mover))
            {
                mover.CanMove = true;
                Dirty(ent.Owner, mover);
            }
        }
    }
    
    private void OnUnstrapped(Entity<VehicleComponent> ent, ref UnstrappedEvent args)
    {
        if(HasComp<RelayInputMoverComponent>(args.Buckle.Owner))
            RemComp<RelayInputMoverComponent>(args.Buckle.Owner);
        ent.Comp.Rider = null;
    }
}