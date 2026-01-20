using Content.Shared._Starlight.Storage;
using Content.Shared.Pinpointer;
using Robust.Shared.Containers;

namespace Content.Server._Starlight.Storage;

public sealed class TwinPointerSystem : SharedTwinPointerSystem
{
    
    [Dependency] private readonly SharedPinpointerSystem _pinpointerSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TwinPointerComponent, MapInitEvent>(Handler);
    }

    private void Handler(EntityUid uid, TwinPointerComponent component, MapInitEvent args)
    {
        if (!_container.TryGetContainer(uid, "storagebase", out var contained))
            return;

        if (contained.Count != 2)
            return;

        var left = contained.ContainedEntities[0];
        var right = contained.ContainedEntities[1];        

        _pinpointerSystem.SetTarget(left, right);
        _pinpointerSystem.SetTarget(right, left);
    }
    
}