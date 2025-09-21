using Content.Shared._FarHorizons.Doors.Components;
using Content.Shared._FarHorizons.Tools.DoorBender.Components;
using Content.Shared.Construction;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;

namespace Content.Shared._FarHorizons.Doors;

public sealed class FHDoorBendSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FHDoorComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(ItemSlotsSystem) }, after: new[] { typeof(SharedConstructionSystem) });
    }
    
    private void OnInteractUsing(EntityUid uid, FHDoorComponent anchorable, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<DoorBenderComponent>(args.Used))
            return;

        if (!_entityManager.TryGetComponent<FHDoorComponent>(args.Target, out var door))
            return;
        
        if (!_entityManager.TryGetComponent<TransformComponent>(args.Target, out var xform))
            return;
        
        xform.LocalRotation += Angle.FromDegrees(90);
        args.Handled = true;
    }
}