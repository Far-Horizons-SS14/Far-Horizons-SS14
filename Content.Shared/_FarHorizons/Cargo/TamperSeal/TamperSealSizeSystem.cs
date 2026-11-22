
using Content.Shared._Starlight.Cargo.TamperSeal.Components;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Cargo.TamperSeal;
public sealed partial class TamperSealSizeSystem : EntitySystem
{
    private static readonly ProtoId<ItemSizePrototype> _fallbackParcelSize = "Ginormous";
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    
    [SubscribeLocalEvent(after:[typeof(SharedItemSystem)])]
    private void OnEntInsertedIntoContainer(Entity<TamperSealableComponent> ent, ref ItemSizeChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var targetItemComp = CompOrNull<ItemComponent>(args.Entity);
        var size = targetItemComp?.Size ?? _fallbackParcelSize;
        _appearance.SetData(ent, TamperSealVisuals.Size, size.Id);
        _appearance.SetData(ent, FactionTamperSealVisuals.Size, size.Id);
    }
}