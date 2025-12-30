using Content.Shared._FarHorizons.Vehicles.Components;
using Content.Shared._FarHorizons.VehicleContainer.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.DragDrop;

namespace Content.Shared._FarHorizons.Vehicles.EntitySystems;

public abstract partial class SharedVehicleSystems : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleComponent, TurnKeysEvent>(OnTurnKeysEvent);
        SubscribeLocalEvent<VehicleComponent, HornActionEvent>(OnHornActionEvent);
        
        SubscribeLocalEvent<VehicleContainerComponent, CanDropTargetEvent>(OnCanDragDrop);
    }

    protected virtual void OnTurnKeysEvent(Entity<VehicleComponent> ent, ref TurnKeysEvent args)
    {
        if(args.Handled || ent.Comp.StartUp == null) return;
        if(ent.Comp.Rider == null) return;
        if(!ent.Comp.Started)
        {
            _audio.PlayPredicted(ent.Comp.StartUp, ent.Owner, ent.Comp.Rider.Value);
        }
        args.Handled = true;
    }

    private void OnHornActionEvent(Entity<VehicleComponent> ent, ref HornActionEvent args)
    {
        if (args.Handled || ent.Comp.HornSound == null)
            return;
        if(ent.Comp.Rider == null) return;
        _audio.PlayPredicted(ent.Comp.HornSound, ent.Owner, ent.Comp.Rider.Value);
        args.Handled = true;
    }

    private void OnCanDragDrop(Entity<VehicleContainerComponent> ent, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop = true;
    }
}