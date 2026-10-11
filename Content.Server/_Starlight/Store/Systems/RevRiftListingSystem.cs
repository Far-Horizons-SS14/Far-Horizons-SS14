using Content.Server.Store.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Revolutionary.Events;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Store.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Store.Systems;

/// <summary>
/// Synchronizes revolutionary store listings with the supply-rift lifecycle.
/// </summary>
public sealed partial class RevRiftListingSystem : EntitySystem
{
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private bool _riftDestroyed;

    public override void Initialize()
    {
        SubscribeLocalEvent<RevSupplyRiftOpenedEvent>(OnRiftStarted);
        SubscribeLocalEvent<RevSupplyRiftDestroyedEvent>(OnRiftDestroyed);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRiftStarted(ref RevSupplyRiftOpenedEvent args)
    {
        UpdateListings();
    }

    private void OnRiftDestroyed(ref RevSupplyRiftDestroyedEvent args)
    {
        _riftDestroyed = true;
        UpdateListings();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _riftDestroyed = false;
        UpdateListings();
    }

    private void UpdateListings()
    {
        var hasActiveRift = EntityQueryEnumerator<RevSupplyRiftComponent>().MoveNext(out _, out _);
        foreach (var listing in _proto.EnumeratePrototypes<ListingPrototype>())
        {
            UpdateCondition(listing.Conditions, hasActiveRift);
        }

        var stores = EntityQueryEnumerator<StoreComponent>();
        while (stores.MoveNext(out var uid, out var store))
        {
            foreach (var listing in store.FullListingsCatalog)
            {
                UpdateCondition(listing.Conditions, hasActiveRift);
            }

            _store.UpdateUserInterface(null, uid, store);
        }
    }

    private void UpdateCondition(List<ListingCondition>? conditions, bool hasActiveRift)
    {
        if (conditions is null)
            return;

        foreach (var condition in conditions)
        {
            if (condition is not RevRiftListingCondition riftCondition)
                continue;

            riftCondition.HasActiveRift = hasActiveRift;
            riftCondition.RiftDestroyed = _riftDestroyed;
        }
    }
}
