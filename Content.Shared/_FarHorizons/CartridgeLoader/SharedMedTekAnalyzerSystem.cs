using Content.Shared._FarHorizons.CartridgeLoader.Cartridges;
using Content.Shared.Actions;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;

namespace Content.Shared._FarHorizons.CartridgeLoader;

public sealed partial class SharedMedTekAnalyzerSystem : EntitySystem
{
    [Dependency] private SharedInteractionSystem _interaction = default!;

    [SubscribeLocalEvent]
    private void OnGetActions(Entity<MedTekAnalyzerComponent> ent, ref GetItemActionsEvent args)
        => args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);

    [SubscribeLocalEvent]
    private void AddVerbAnalyzer(Entity<MedTekAnalyzerComponent> ent, ref InventoryRelayedEvent<GetVerbsEvent<InnateVerb>> args)
    {
        if (!args.Args.CanInteract || !args.Args.CanAccess)
            return;
        if (!HasComp<DamageableComponent>(args.Args.Target) || !HasComp<MobStateComponent>(args.Args.Target))
            return;

        var user = args.Args.User;
        var target = args.Args.Target;

        if (TryComp(target, out TransformComponent? targetTransform))
        {
            var patientCoordinates = targetTransform.Coordinates;
            InnateVerb verb = new()
            {
                Act = () => _interaction.InteractDoAfter(user, ent.Owner, target, patientCoordinates, true), 
                Text = "Analyze Patient",
                IconEntity = GetNetEntity(ent),
                Priority = 2,
            };
            args.Args.Verbs.Add(verb);
        }
    }   
    
    [SubscribeLocalEvent]
    private void OnMedTekAction(Entity<MedTekAnalyzerComponent> ent, ref MedTekActionEvent args)
    {
        var user = args.Performer;
        var target = args.Target;
        if (TryComp(target, out TransformComponent? targetTransform))
        {
            var patientCoordinates = targetTransform.Coordinates;
            _interaction.InteractDoAfter(user, ent.Owner, target, patientCoordinates, true);
        }
    }
}