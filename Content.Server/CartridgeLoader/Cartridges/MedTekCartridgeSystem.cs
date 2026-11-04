using Content.Server.Medical.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared._FarHorizons.CartridgeLoader; //FH

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class MedTekCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedTekCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
        SubscribeLocalEvent<MedTekCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);
    }

    private void OnCartridgeAdded(Entity<MedTekCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        EnsureComp<HealthAnalyzerComponent>(args.Loader);
        EnsureComp<MedTekAnalyzerComponent>(args.Loader); //FH-Edit
    }

    private void OnCartridgeRemoved(Entity<MedTekCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        // only remove when the program itself is removed
        if (!_cartridgeLoaderSystem.HasProgram<MedTekCartridgeComponent>(args.Loader))
        {
            RemComp<HealthAnalyzerComponent>(args.Loader);
            RemComp<MedTekAnalyzerComponent>(args.Loader); //FH-Edit
        }
    }
}