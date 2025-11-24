using Content.Server.FarHorizons.Tools.Shipyard.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Verbs;
using Content.Shared.Inventory;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Shared.Mobs.Components;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class HullSenseCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HullSenseCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
        SubscribeLocalEvent<HullSenseCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);
        SubscribeLocalEvent<IntegrityAnalyzerComponent, InventoryRelayedEvent<GetVerbsEvent<InnateVerb>>>(AddVerbAnalyzer);
    }

    private void OnCartridgeAdded(Entity<HullSenseCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        var healthAnalyzer = EnsureComp<IntegrityAnalyzerComponent>(args.Loader);
    }

    private void OnCartridgeRemoved(Entity<HullSenseCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        // only remove when the program itself is removed
        if (!_cartridgeLoaderSystem.HasProgram<HullSenseCartridgeComponent>(args.Loader))
        {
            RemComp<IntegrityAnalyzerComponent>(args.Loader);
        }
    }

    private void AddVerbAnalyzer(Entity<IntegrityAnalyzerComponent> ent, ref InventoryRelayedEvent<GetVerbsEvent<InnateVerb>> args)
    {
        if (!args.Args.CanInteract || !args.Args.CanAccess)
            return;
        if (!HasComp<DamageableComponent>(args.Args.Target) || HasComp<MobStateComponent>(args.Args.Target))
            return;

        var user = args.Args.User;
        var target = args.Args.Target;

        if (TryComp(target, out TransformComponent? targetTransform))
        {
            var patientCoordinates = targetTransform.Coordinates;
            InnateVerb verb = new()
            {
                Act = () => _interactionSystem.InteractDoAfter(user, ent.Owner, target, patientCoordinates, true), // Setting canReach to true, because if it's false - args.Args.CanAccess will be false and this code won't run
                Text = "Analyze Structure",
                IconEntity = GetNetEntity(ent),
                Priority = 2,
            };
            args.Args.Verbs.Add(verb);
        }
    }    
}
