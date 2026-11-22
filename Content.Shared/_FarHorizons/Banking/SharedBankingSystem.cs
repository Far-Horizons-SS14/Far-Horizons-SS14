using Content.Shared._FarHorizons.Banking.Components;
using Content.Shared.Alert;
using Content.Shared.Cargo;
using Content.Shared.CartridgeLoader;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Station;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Banking;

public abstract partial class SharedBankingSystem : EntitySystem
{
    [Dependency] protected ItemSlotsSystem ItemSlots = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedCargoSystem _cargo = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public static int GetRemainingLimit(BankAccountBalance balance) => 
        Math.Min(balance.Limit - balance.Spent, balance.Balance);

    [SubscribeLocalEvent]
    private void OnUiReady(Entity<FrontierBankAppComponent> ent, ref CartridgeUiReadyEvent args) => 
        UpdateBankAppUi(ent, args.Loader, args.Actor);
    
    [SubscribeLocalEvent]
    private void OnCredstickInserted(Entity<CredstickComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!TryComp<CartridgeLoaderComponent>(args.Container.Owner, out var cartridgeLoader))
            return;
        
        RefreshCredstickState((args.Container.Owner, cartridgeLoader), ent);
    }

    [SubscribeLocalEvent]
    private void OnCredstickEjected(Entity<CredstickComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!TryComp<CartridgeLoaderComponent>(args.Container.Owner, out var cartridgeLoader))
            return;
        
        RefreshCredstickState((args.Container.Owner, cartridgeLoader), null);
    }

    protected virtual void UpdateBankAppUi(Entity<FrontierBankAppComponent> ent, EntityUid loader, EntityUid actor) {}
    protected virtual void RefreshCredstickState(Entity<CartridgeLoaderComponent> ent, Entity<CredstickComponent>? credstick) {}
    
    public abstract bool ChangeBalance(EntityUid ent, int delta);
}

[Serializable, NetSerializable]
public record struct BankAccountBalance(int Balance, int Limit, int Spent);
