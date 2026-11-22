
using Content.Shared._FarHorizons.Banking.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Store.Components;
using Robust.Shared.Containers;

namespace Content.Server._FarHorizons.Banking;

public sealed partial class BankingSystem
{
    [SubscribeLocalEvent]
    private void OnCredstickStoreInit(Entity<CredstickStoreComponent> ent, ref MapInitEvent args)
    {
        var store = EnsureComp<StoreComponent>(ent);
        store.Balance = new();
        ItemSlots.AddItemSlot(ent, ent.Comp.SlotId, ent.Comp.CredstickSlot);
    }

    [SubscribeLocalEvent]
    private void OnCredstickStoreInsert(Entity<CredstickStoreComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<CredstickComponent>(args.Entity, out var credstickComp) ||
            !TryComp<StoreComponent>(ent, out var store))
            return;
        
        Entity<CredstickComponent> credstick = (args.Entity, credstickComp);
        var balance = CredstickGetBalance(credstick.AsNullable());

        if (store.Balance.ContainsKey(ent.Comp.Currency))
            store.Balance[ent.Comp.Currency] = FixedPoint2.New(balance);
        else
            store.Balance.Add(ent.Comp.Currency, FixedPoint2.New(balance));

        CredstickSetBalance(credstick.AsNullable(), 0);

        _store.UpdateUserInterface(null, ent);
    }

    [SubscribeLocalEvent]
    private void OnCredstickStoreEject(Entity<CredstickStoreComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryComp<CredstickComponent>(args.Entity, out var credstickComp) ||
            !TryComp<StoreComponent>(ent, out var store) ||
            !store.Balance.ContainsKey(ent.Comp.Currency))
            return;
        
        Entity<CredstickComponent> credstick = (args.Entity, credstickComp);
        var balance = (int)store.Balance[ent.Comp.Currency];

        CredstickSetBalance(credstick.AsNullable(), balance);
        store.Balance[ent.Comp.Currency] = FixedPoint2.Zero;

        _store.UpdateUserInterface(null, ent);
    }
}