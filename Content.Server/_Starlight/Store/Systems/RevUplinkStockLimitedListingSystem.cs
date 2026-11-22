using Content.Server.Store.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Store.Conditions;
using Content.Shared.Store.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Store.Systems;

/// <summary>
/// Synchronizes stock-limited revolutionary listings across every revolutionary uplink.
/// </summary>
public sealed partial class RevUplinkStockLimitedListingSystem : EntitySystem
{
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly Dictionary<string, (int Remaining, string? LastPurchaser)> _stock = new();
    private readonly Dictionary<(EntityUid Store, EntityUid Buyer, string Listing), int> _reservations = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<StorePurchaseAttemptEvent>(OnStorePurchaseAttempt);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStoreBuyFinished);
        SubscribeLocalEvent<StoreListingsRefreshedEvent>(OnStoreListingsRefreshed);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnStorePurchaseAttempt(ref StorePurchaseAttemptEvent ev)
    {
        if (!TryGetStockCondition(ev.StoreEntity, ev.ListingId, out var stock))
            return;

        var current = _stock.TryGetValue(ev.ListingId, out var existing)
            ? existing.Remaining
            : stock.StockLimit;
        if (current <= 0)
        {
            ev.Cancel = true;
            return;
        }

        UpdateStock(ev.ListingId, current - 1, existing.LastPurchaser);
        var key = (ev.StoreEntity, ev.Buyer, ev.ListingId);
        _reservations[key] = _reservations.GetValueOrDefault(key) + 1;
    }

    private void OnStoreListingsRefreshed(ref StoreListingsRefreshedEvent ev)
    {
        if (!TryComp(ev.Store, out StoreComponent? store))
            return;

        SynchronizeStoreListings(store);
    }

    private void OnStoreBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        if (ev.PurchasedItem.Conditions is null)
            return;

        foreach (var condition in ev.PurchasedItem.Conditions)
        {
            if (condition is not StockLimitedListingCondition stock)
                continue;

            var key = (ev.StoreUid, ev.Buyer, ev.PurchasedItem.ID);
            if (!_reservations.TryGetValue(key, out var reserved) || reserved == 0)
                return;

            if (reserved == 1)
                _reservations.Remove(key);
            else
                _reservations[key] = reserved - 1;

            var remaining = _stock.TryGetValue(ev.PurchasedItem.ID, out var existing)
                ? existing.Remaining
                : stock.StockLimit - 1;
            var purchaser = TryComp(ev.Buyer, out MetaDataComponent? metadata)
                ? metadata.EntityName
                : null;
            UpdateStock(ev.PurchasedItem.ID, remaining, purchaser);
            return;
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _stock.Clear();
        _reservations.Clear();

        var stores = EntityQueryEnumerator<StoreComponent>();
        while (stores.MoveNext(out var uid, out var store))
        {
            SynchronizeStoreListings(store);
            _store.UpdateUserInterface(null, uid, store);
        }
    }

    private void UpdateStock(string listingId, int remaining, string? purchaser)
    {
        _stock[listingId] = (remaining, purchaser);

        var stores = EntityQueryEnumerator<StoreComponent>();
        while (stores.MoveNext(out var uid, out var store))
        {
            foreach (var listing in store.FullListingsCatalog)
            {
                if (listing.ID != listingId || listing.Conditions is null)
                    continue;

                foreach (var condition in listing.Conditions)
                {
                    if (condition is StockLimitedListingCondition stock)
                    {
                        stock.CurrentStock = remaining;
                        stock.LastPurchaser = purchaser;
                        UpdateListingPresentation(listing, stock);
                    }
                }
            }

            _store.UpdateUserInterface(null, uid, store);
        }
    }

    private void SynchronizeStoreListings(StoreComponent store)
    {
        foreach (var listing in store.FullListingsCatalog)
        {
            if (listing.Conditions is null)
                continue;

            foreach (var condition in listing.Conditions)
            {
                if (condition is not StockLimitedListingCondition stock)
                    continue;

                if (!_stock.TryGetValue(listing.ID, out var value))
                {
                    value = (stock.StockLimit, null);
                    _stock[listing.ID] = value;
                }

                stock.CurrentStock = value.Remaining;
                stock.LastPurchaser = value.LastPurchaser;
                UpdateListingPresentation(listing, stock);
            }
        }
    }

    private bool TryGetStockCondition(EntityUid storeUid, string listingId, out StockLimitedListingCondition condition)
    {
        if (TryComp(storeUid, out StoreComponent? store))
        {
            foreach (var listing in store.FullListingsCatalog)
            {
                if (listing.ID != listingId || listing.Conditions is null)
                    continue;

                foreach (var listingCondition in listing.Conditions)
                {
                    if (listingCondition is StockLimitedListingCondition stock)
                    {
                        condition = stock;
                        return true;
                    }
                }
            }
        }

        condition = null!;
        return false;
    }

    private void UpdateListingPresentation(ListingData listing, StockLimitedListingCondition stock)
    {
        if (!_proto.TryIndex<ListingPrototype>(listing.ID, out var prototype))
            return;

        if (prototype.Name is { } name)
        {
            var stockText = stock.CurrentStock == 0
                ? Loc.GetString("store-ui-button-out-of-stock")
                : $"{stock.CurrentStock}/{stock.StockLimit}";
            listing.Name = $"{Loc.GetString(name)} ({stockText})";
        }

        if (prototype.Description is { } description)
        {
            listing.Description = string.IsNullOrEmpty(stock.LastPurchaser)
                ? Loc.GetString(description)
                : $"{Loc.GetString(description)} Last purchased by: {stock.LastPurchaser}";
        }
    }
}
