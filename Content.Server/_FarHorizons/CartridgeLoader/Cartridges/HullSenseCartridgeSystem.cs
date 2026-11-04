using Content.Server.FarHorizons.Tools.Shipyard.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared._FarHorizons.CartridgeLoader;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class HullSenseCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoaderSystem = default!;

    [SubscribeLocalEvent]
    private void OnCartridgeAdded(Entity<HullSenseCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        EnsureComp<IntegrityAnalyzerComponent>(args.Loader);
        EnsureComp<HullSenseAnalyzerComponent>(args.Loader);
    }

    [SubscribeLocalEvent]
    private void OnCartridgeRemoved(Entity<HullSenseCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        // only remove when the program itself is removed
        if (!_cartridgeLoaderSystem.HasProgram<HullSenseCartridgeComponent>(args.Loader))
        {
            RemComp<IntegrityAnalyzerComponent>(args.Loader);
            RemComp<HullSenseAnalyzerComponent>(args.Loader);
        }
    }
}
