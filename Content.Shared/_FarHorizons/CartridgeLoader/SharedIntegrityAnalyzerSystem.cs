
using Content.Shared._FarHorizons.CartridgeLoader.Cartridges;
using Content.Shared.Actions;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;

namespace Content.Shared._FarHorizons.CartridgeLoader;
public sealed partial class SharedIntegrityAnalyzerSystem : EntitySystem
{
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;

    [SubscribeLocalEvent]
    private void AddVerbAnalyzer(Entity<HullSenseAnalyzerComponent> ent, ref InventoryRelayedEvent<GetVerbsEvent<InnateVerb>> args)
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

    [SubscribeLocalEvent]
    private void OnGetActions(Entity<HullSenseAnalyzerComponent> ent, ref GetItemActionsEvent args) 
        => args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);

    [SubscribeLocalEvent]
    private void OnHullSenseAction(Entity<HullSenseAnalyzerComponent> ent, ref HullSenseActionEvent args)
    {
        var user = args.Performer;
        var target = args.Target;
        if (TryComp(target, out TransformComponent? targetTransform))
        {
            var patientCoordinates = targetTransform.Coordinates;
            _interactionSystem.InteractDoAfter(user, ent.Owner, target, patientCoordinates, true);
        }
    }
}