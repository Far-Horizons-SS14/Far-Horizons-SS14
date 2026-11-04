using Content.Shared._FarHorizons.CartridgeLoader.Cartridges;
using Content.Shared.Actions;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;

namespace Content.Shared._FarHorizons.CartridgeLoader;

public sealed partial class SharedCriminalAnalyzerSystem : EntitySystem
{

    [SubscribeLocalEvent]
    private void AddVerbCheckCrime(Entity<CrimeAnalyzerComponent> ent, ref InventoryRelayedEvent<GetVerbsEvent<InnateVerb>> args)
    {
        if (!HasComp<MobStateComponent>(args.Args.Target) || !TryComp<IdentityComponent>(args.Args.Target, out var idComp))
            return;

        var target = args.Args.Target;
        var user = args.Args.User;
        var item = ent.Owner;

        InnateVerb verb = new()
        {
            Act = () =>
            {
                var ev = new CheckCriminalEvent(user, target);
                RaiseLocalEvent(item, ref ev);
            },
            Text = "Check Crimes",
            IconEntity = GetNetEntity(ent),
            Priority = 2,
        };
        args.Args.Verbs.Add(verb);
    }

    [SubscribeLocalEvent]
    private void OnGetActions(Entity<CrimeAnalyzerComponent> ent, ref GetItemActionsEvent args) 
        => args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);

    [SubscribeLocalEvent]
    private void OnCrimeCheckAction(Entity<CrimeAnalyzerComponent> ent, ref CrimeCheckActionEvent args)
    {
        var ev = new CheckCriminalEvent(args.Performer, args.Target);
        RaiseLocalEvent(ent, ref ev);
    }
}