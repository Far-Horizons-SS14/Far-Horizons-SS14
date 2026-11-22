using Content.Server.Popups;
using Content.Server.Revolutionary.Components;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Store.Components;
using Content.Shared.Store.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Implants;

/// <summary>
/// Far Horizons: manages the detached stores backing USSP uplink implants.
/// </summary>
public sealed partial class USSPUplinkSystem : EntitySystem
{
    private static readonly EntProtoId<StoreComponent> USSPUplinkStore = "StorePresetRemoteUSSPUplink";

    [Dependency] private StoreSystem _storeSystem = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SubdermalImplantSystem _implant = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStoreBuyFinished);
        SubscribeLocalEvent<USSPUplinkImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
    }

    private Entity<StoreComponent>? FindStoreForOwner(EntityUid owner)
    {
        var storeQuery = EntityQueryEnumerator<USSPUplinkOwnerComponent, StoreComponent>();
        while (storeQuery.MoveNext(out var storeOwner, out var uplinkComp, out var storeComp))
        {
            if (uplinkComp.OwnerUid != owner)
                continue;

            return (storeOwner, storeComp);
        }

        return null;
    }

    private static void EnsureCurrencies(StoreComponent store)
    {
        store.Balance.TryAdd("Telebond", FixedPoint2.Zero);
        store.Balance.TryAdd("Conversion", FixedPoint2.Zero);
    }

    private FixedPoint2 GetGlobalConversion()
    {
        var conversion = FixedPoint2.Zero;
        var stores = new HashSet<EntityUid>();
        var implantQuery = EntityQueryEnumerator<USSPUplinkImplantComponent>();
        while (implantQuery.MoveNext(out var implant, out _))
        {
            if (!TryGetStore(implant, out var store) || !stores.Add(store.Owner))
                continue;

            EnsureCurrencies(store.Comp);
            conversion = store.Comp.Balance["Conversion"] > conversion
                ? store.Comp.Balance["Conversion"]
                : conversion;
        }

        return conversion;
    }

    /// <summary>
    /// Links an implant to its head revolutionary's backing store. A store is only reused when it
    /// belongs to the same owner, so claiming an implant cannot steal another owner's store.
    /// </summary>
    private Entity<StoreComponent>? LinkStore(EntityUid implant, EntityUid owner, EntityUid? previousOwner, out bool created)
    {
        created = false;
        if (!TryComp<RemoteStoreComponent>(implant, out var remote))
            return null;

        var previousStore = remote.Store is { } previousStoreUid &&
                            TryComp<StoreComponent>(previousStoreUid, out var previousStoreComp)
            ? (previousStoreUid, previousStoreComp)
            : (Entity<StoreComponent>?) null;

        var store = FindStoreForOwner(owner);
        if (store == null && previousOwner == owner)
            store = previousStore;

        if (store == null)
        {
            var globalConversion = GetGlobalConversion();
            var storeUid = Spawn(USSPUplinkStore, MapCoordinates.Nullspace);
            store = (storeUid, Comp<StoreComponent>(storeUid));
            EnsureCurrencies(store.Value.Comp);
            store.Value.Comp.Balance["Conversion"] = globalConversion;
            EnsureComp<USSPUplinkOwnerComponent>(store.Value.Owner).OwnerUid = owner;
            created = true;
        }

        EnsureCurrencies(store.Value.Comp);
        if (previousStore?.Owner != store.Value.Owner)
        {
            _storeSystem.SetRemoteStore((implant, remote), store.Value.Owner);
            Dirty(implant, remote);
        }

        if (_mind.TryGetMind(owner, out var mind, out _))
            store.Value.Comp.AccountOwner = mind;

        return store;
    }

    private bool TryGetStore(EntityUid implant, out Entity<StoreComponent> store)
    {
        if (_storeSystem.TryGetStore(implant, out var result) && result is { } resolved)
        {
            store = resolved;
            return true;
        }

        store = default;
        return false;
    }

    private void OnImplantImplanted(Entity<USSPUplinkImplantComponent> implant, ref ImplantImplantedEvent args)
    {
        var previousOwner = CompOrNull<USSPUplinkOwnerComponent>(implant)?.OwnerUid;
        EntityUid? ownerUid;
        if (HasComp<HeadRevolutionaryComponent>(args.Implanted))
            ownerUid = args.Implanted;
        else if (HasComp<RevolutionaryComponent>(args.Implanted))
            ownerUid = previousOwner ?? FindOwner(implant, args.Implanted);
        else
            return;

        if (ownerUid == null)
            return;

        if (previousOwner is { } formerOwner &&
            formerOwner != ownerUid &&
            CompOrNull<HeadRevolutionaryImplantComponent>(formerOwner)?.ImplantUid == implant)
        {
            Comp<HeadRevolutionaryImplantComponent>(formerOwner).ImplantUid = null;
        }

        EnsureComp<USSPUplinkOwnerComponent>(implant).OwnerUid = ownerUid;
        if (LinkStore(implant, ownerUid.Value, previousOwner, out var created) is not { } store)
            return;

        if (HasComp<HeadRevolutionaryComponent>(args.Implanted))
        {
            var headImplant = EnsureComp<HeadRevolutionaryImplantComponent>(args.Implanted);
            headImplant.ImplantUid = implant;

            var convertedCount = 0;
            if (created)
            {
                foreach (var (_, convertedBy) in EntityQuery<RevolutionaryComponent, RevolutionaryConvertedByComponent>())
                {
                    if (convertedBy.ConverterUid != args.Implanted)
                        continue;

                    convertedCount++;
                }

                if (convertedCount > 0)
                {
                    _storeSystem.TryAddCurrency(
                        new Dictionary<string, FixedPoint2> { { "Telebond", FixedPoint2.New(2 * convertedCount) } },
                        store.Owner,
                        store.Comp);
                }
            }

            var telebonds = store.Comp.Balance["Telebond"];
            var conversions = store.Comp.Balance["Conversion"];
            var previousConversions = convertedCount > 0 ? $" (+{convertedCount} from previous conversions)" : string.Empty;
            _popup.PopupEntity(
                Loc.GetString($"Implanted! Current Telebonds: {telebonds}{previousConversions}, Conversions: {conversions}"),
                args.Implanted,
                args.Implanted,
                PopupType.Medium);

            if (previousOwner != null && previousOwner != args.Implanted)
            {
                _popup.PopupEntity(
                    Loc.GetString($"Your uplink has been claimed by {Identity.Name(args.Implanted, EntityManager)}"),
                    previousOwner.Value,
                    previousOwner.Value,
                    PopupType.Medium);
            }

            return;
        }

        EnsureComp<HeadRevolutionaryImplantComponent>(args.Implanted).ImplantUid = implant;

        if (ownerUid != null && ownerUid != args.Implanted)
        {
            _popup.PopupEntity(
                Loc.GetString($"Your uplink has been implanted in {Identity.Name(args.Implanted, EntityManager)}"),
                ownerUid.Value,
                ownerUid.Value,
                PopupType.Medium);
        }
    }

    private EntityUid? FindOwner(EntityUid implant, EntityUid recipient)
    {
        if (CompOrNull<RevolutionaryConvertedByComponent>(recipient)?.ConverterUid is { } converter &&
            HasComp<HeadRevolutionaryComponent>(converter))
        {
            return converter;
        }

        var headRevImplantQuery = EntityQueryEnumerator<HeadRevolutionaryComponent, HeadRevolutionaryImplantComponent>();
        while (headRevImplantQuery.MoveNext(out var head, out _, out var headImplant))
        {
            if (headImplant.ImplantUid == implant)
                return head;
        }

        var headRevQuery = EntityQueryEnumerator<HeadRevolutionaryComponent>();
        while (headRevQuery.MoveNext(out var head, out _))
        {
            if (_implant.TryGetImplants(head, out var implants) && implants.Contains(implant))
                return head;
        }

        return null;
    }

    private void OnStoreBuyFinished(ref StoreBuyFinishedEvent args)
    {
        if (!TryComp(args.StoreUid, out StoreComponent? store))
            return;

        if (store.CurrencyWhitelist.Contains("Conversion"))
        {
            var spent = args.PurchasedItem.Cost.GetValueOrDefault("Conversion", FixedPoint2.Zero);
            if (spent > FixedPoint2.Zero)
                _storeSystem.TryAddCurrency(new Dictionary<string, FixedPoint2> { { "Conversion", spent } }, args.StoreUid, store);
        }
    }

    public void AddConversionToAllHeadRevs(
        StoreSystem storeSystem,
        IReadOnlyDictionary<EntityUid, EntityUid> headUplinks)
    {
        var stores = new HashSet<EntityUid>();
        var implantQuery = EntityQueryEnumerator<USSPUplinkImplantComponent>();
        while (implantQuery.MoveNext(out var implant, out _))
        {
            if (!TryGetStore(implant, out var store) || !stores.Add(store.Owner))
                continue;

            EnsureCurrencies(store.Comp);
            storeSystem.TryAddCurrency(
                new Dictionary<string, FixedPoint2> { { "Conversion", FixedPoint2.New(1) } },
                store.Owner,
                store.Comp);
        }

        // Radio uplinks are their own Stores, rather than RemoteStores backed by an implant.
        foreach (var uplink in headUplinks.Values)
        {
            if (!TryGetStore(uplink, out var store) || !stores.Add(store.Owner))
                continue;

            EnsureCurrencies(store.Comp);
            storeSystem.TryAddCurrency(
                new Dictionary<string, FixedPoint2> { { "Conversion", FixedPoint2.New(1) } },
                store.Owner,
                store.Comp);
        }

        var headRevQuery = EntityQueryEnumerator<HeadRevolutionaryComponent>();
        while (headRevQuery.MoveNext(out var head, out _))
        {
            var conversion = FixedPoint2.New(1);
            if (headUplinks.TryGetValue(head, out var uplink) &&
                TryGetStore(uplink, out var uplinkStore))
            {
                conversion = uplinkStore.Comp.Balance.GetValueOrDefault("Conversion", conversion);
            }
            else if (FindStoreForOwner(head) is { } store)
            {
                conversion = store.Comp.Balance.GetValueOrDefault("Conversion", conversion);
            }
            else
            {
                continue;
            }

            _popup.PopupEntity(
                Loc.GetString($"+1 Conversion (Total: {conversion})"),
                head,
                head,
                PopupType.Medium);
        }
    }
}
