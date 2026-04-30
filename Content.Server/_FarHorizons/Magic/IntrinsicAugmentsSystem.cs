using Content.Server.Actions;
using Content.Shared._FarHorizons.Magic;
using Content.Shared.FixedPoint;
using Content.Shared.Store.Components;
using Content.Shared.Store.Events;
using Robust.Server.GameObjects;

namespace Content.Server._FarHorizons.Magic;

public sealed class IntrinsicAugmentsSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntrinsicAugmentsComponent, MapInitEvent>(OnCantripInit);
        SubscribeLocalEvent<StorePurchaseCompletedEvent>(OnPurchase);
    }

    private void OnPurchase(ref StorePurchaseCompletedEvent ev)
    {
        if (!TryComp<IntrinsicAugmentsComponent>(ev.Buyer, out var comp) ||
            comp.Store == null ||
            comp.Store.Balance.Values.Sum() > 0)
            return;

        _ui.CloseUi(ev.Buyer, comp.UiKey, ev.Buyer);

        var actions = _actions.GetActions(ev.Buyer);
        foreach (var action in actions)
        {
            var meta = MetaData(action);
            if (meta.EntityPrototype != null &&
                meta.EntityPrototype == comp.Action)
                _actions.RemoveAction(action.AsNullable());
        }

        RemCompDeferred<IntrinsicAugmentsComponent>(ev.Buyer);
    }

    private void OnCantripInit(Entity<IntrinsicAugmentsComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ent.Comp.Action);
        EntityManager.AddComponents(ent, ent.Comp.AddComponents);
        ent.Comp.Store = EnsureComp<StoreComponent>(ent);
    }
}