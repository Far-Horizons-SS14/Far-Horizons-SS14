using Content.Shared._FarHorizons.Fluids.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Content.Shared.Inventory;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Fluids;

public abstract partial class SharedFluidFootprintSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] protected SharedSolutionContainerSystem Solution = default!;
    [Dependency] protected SharedTransformSystem TransformSys = default!;

    private const string CONTAINER_ENTITY = "FluidFootprintContainer";
    
    [SubscribeLocalEvent]
    private void OnBootModifyFootprint(Entity<BootFootprintOverrideComponent> ent, ref InventoryRelayedEvent<BootFootprintModifyEvent> args) =>
        args.Args.Footprint = ent.Comp.Footprint;

    public void AttemptClean(Entity<AbsorbentComponent> absorber, EntityUid target, EntityUid user)
    {
        if (!TryComp<FluidFootprintContainerComponent>(target, out var container))
            return;

        PredictedQueueDel(target);

        if (container.CleanEffect != null &&
            TransformSys.TryGetMapOrGridCoordinates(target, out var pos))
            PredictedSpawnAtPosition(container.CleanEffect, pos.Value);
        
        _audio.PlayPredicted(absorber.Comp.PickupSound, absorber, user);
    }

    protected virtual void UpdateSprite(Entity<FluidFootprintContainerComponent> ent) { } // Clientside only
}