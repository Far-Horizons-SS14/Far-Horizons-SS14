using System.Linq;
using Content.Server.PDA.Ringer;
using Content.Server.Store.Systems;
using Content.Server.StoreDiscount.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared._FarHorizons.PDA;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Traitor.Uplink;

public sealed partial class UplinkSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private SharedSubdermalImplantSystem _subdermalImplant = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private RingerSystem _ringer = default!;

    public static readonly EntProtoId<StoreComponent> TraitorUplinkStore = "StorePresetRemoteUplink";
    public static readonly ProtoId<CurrencyPrototype> TelecrystalCurrencyPrototype = "Telecrystal";
    private static readonly EntProtoId FallbackUplinkImplant = "UplinkImplant";
    private static readonly ProtoId<ListingPrototype> FallbackUplinkCatalog = "UplinkUplinkImplanter";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemoteStoreComponent, ImplantImplantedEvent>(OnRemoteStoreImplanted);
    }

    private void OnRemoteStoreImplanted(Entity<RemoteStoreComponent> entity, ref ImplantImplantedEvent args)
    {
        // Far Horizons: USSP implants create and manage their own detached revolutionary store.
        if (MetaData(entity).EntityPrototype?.ID == "USSPUplinkImplant")
            return;

        if (_mind.GetMind(args.Implanted) is not { } mind )
            return;

        var storeEnumerator = EntityQueryEnumerator<RingerAccessUplinkComponent, StoreComponent>();
        while (storeEnumerator.MoveNext(out var uid, out _, out var store))
        {
            if (store.AccountOwner != mind)
                continue;

            entity.Comp.Store = uid;
            return;
        }

        // If we didn't have an uplink, make an empty one.
        var proto = entity.Comp.Proto ?? TraitorUplinkStore; // Far Horizons
        var currency = entity.Comp.Currency ?? TelecrystalCurrencyPrototype; // Far Horizons
        entity.Comp.Store = Spawn(proto, MapCoordinates.Nullspace); // Far Horizons
        Dirty(entity, entity.Comp); // FH - If you don't dirty the entity, the client doesn't know about the store and can't use it.
        SetUplink(args.Implanted, entity.Comp.Store.Value, 0, false, currency); // Far Horizons
        Log.Warning($"{ToPrettyString(args.Implanted)} did not have an uplink when they were implanted."); // FH - Error to Warning so that we can explicitly do this in a test
    }

    /// <summary>
    /// Adds an uplink to the target
    /// </summary>
    /// <param name="user">The person who is getting the uplink</param>
    /// <param name="balance">The amount of currency on the uplink. If null, will just use the amount specified in the preset.</param>
    /// <param name="code">The code which was generated, if any.</param>
    /// <param name="uplinkEntity">The entity that will actually have the uplink functionality. Defaults to the PDA if null.</param>
    /// <param name="giveDiscounts">Marker that enables discounts for uplink items.</param>
    /// <param name="bindToPda">Binds the uplink to the specific uplink entity.</param>
    /// <returns>Whether the uplink was added successfully to a PDA, implant or not at all.</returns>
    public AddUplinkResult AddUplink(
        EntityUid user,
        FixedPoint2 balance,
        out Note[]? code,
        EntityUid? uplinkEntity = null,
        bool giveDiscounts = false,
        bool bindToPda = false,
        EntProtoId? proto = null, // Far Horizons
        EntProtoId? implantProto = null, // Far Horizons
        ProtoId<ListingPrototype>? uplinkCatalog = null, // Far Horizons
        ProtoId<CurrencyPrototype>? currency = null) // Far Horizons
    {
        code = null;

        proto ??= TraitorUplinkStore; // Far Horizons
        currency ??= TelecrystalCurrencyPrototype; // Far Horizons
        var storeEntity = Spawn(proto, MapCoordinates.Nullspace); // Far Horizons
        if (TryAddEntityUplink(user, balance, out var generatedCode, uplinkEntity, storeEntity, currency.Value, giveDiscounts, bindToPda)) // Far Horizons
        {
            code = generatedCode;
            return AddUplinkResult.Pda;
        }

        implantProto ??= FallbackUplinkImplant; // Far Horizons
        uplinkCatalog ??= FallbackUplinkCatalog; // Far Horizons
        if (TryImplantUplink(user, storeEntity, balance, giveDiscounts, implantProto.Value, uplinkCatalog.Value, currency.Value)) // Far Horizons
        {
            return AddUplinkResult.Implant;
        }

        Del(storeEntity);
        return AddUplinkResult.Failure;
    }

    public bool TryAddEntityUplink(
        EntityUid user,
        FixedPoint2 balance,
        out Note[]? code,
        EntityUid? uplinkEntity,
        EntityUid storeEntity,
        ProtoId<CurrencyPrototype> currency, // Far Horizons
        bool giveDiscounts = false,
        bool bindToPda = false)
    {
        code = null;
        uplinkEntity ??= FindUplinkTarget(user);

        if (uplinkEntity == null)
            return false;

        var ev = new GenerateUplinkCodeEvent();
        RaiseLocalEvent(storeEntity, ref ev);

        if (ev.Code == null)
        {
            QueueDel(storeEntity);
            return false;
        }

        code = ev.Code;

        if (bindToPda)
        {
            var accessComp = EnsureComp<RingerAccessUplinkComponent>(storeEntity);
            _ringer.SetBoundUplinkEntity((storeEntity, accessComp), uplinkEntity.Value);
        }

        SetUplink(user, storeEntity, balance, giveDiscounts, currency); // Far Horizons
        // Far Horizons: Only PDAs given a traitor store may use any traitor's ringer code.
        if (HasComp<PdaComponent>(uplinkEntity))
            EnsureComp<RingerCapablePDAComponent>(uplinkEntity.Value);

        return true;
    }

    /// <summary>
    /// Configure TC for the uplink
    /// </summary>
    private void SetUplink(EntityUid user, EntityUid store, FixedPoint2 balance, bool giveDiscounts, ProtoId<CurrencyPrototype> currency) // Far Horizons
    {
        if (!_mind.TryGetMind(user, out var mind, out _))
            return;

        var storeComp = EnsureComp<StoreComponent>(store);

        storeComp.AccountOwner = mind;

        storeComp.Balance.Clear();
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { currency, balance } }, // Far Horizons
            store,
            storeComp);

        var uplinkInitializedEvent = new StoreInitializedEvent(
            TargetUser: mind,
            Store: store,
            UseDiscounts: giveDiscounts,
            Listings: _store.GetAvailableListings(mind, store, storeComp)
                .ToArray());
        RaiseLocalEvent(ref uplinkInitializedEvent);
    }

    /// <summary>
    /// Implant an uplink as a fallback measure if the traitor had no PDA
    /// </summary>
    public bool TryImplantUplink(EntityUid user, EntityUid storeEntity, FixedPoint2 balance, bool giveDiscounts, EntProtoId implantProto, ProtoId<ListingPrototype> catalogProto, ProtoId<CurrencyPrototype> currency) // Far Horizons
    {
        if (!_proto.Resolve(catalogProto, out var catalog)) // Far Horizons
            return false;

        if (!catalog.Cost.TryGetValue(currency, out var cost)) // Far Horizons
            return false;

        if (balance < cost) // Can't use Math functions on FixedPoint2
            balance = 0;
        else
            balance = balance - cost;

        SetUplink(user, storeEntity, balance, giveDiscounts, currency); // Far Horizons
        var implant = _subdermalImplant.AddImplant(user, implantProto); // Far Horizons

        if (!HasComp<RemoteStoreComponent>(implant))
        {
            Log.Error($"Implant does not have the store component {implant}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Finds the entity that can hold an uplink for a user.
    /// Usually this is a pda in their pda slot, but can also be in their hands. (but not pockets or inside bag, etc.)
    /// </summary>
    public EntityUid? FindUplinkTarget(EntityUid user)
    {
        // Try to find PDA in inventory
        if (_inventorySystem.TryGetContainerSlotEnumerator(user, out var containerSlotEnumerator))
        {
            while (containerSlotEnumerator.MoveNext(out var containerSlot))
            {
                var pdaUid = containerSlot.ContainedEntity;

                if (HasComp<PdaComponent>(pdaUid) && HasComp<RemoteStoreComponent>(pdaUid))
                    return pdaUid.Value;
            }
        }

        // Also check hands
        foreach (var item in _handsSystem.EnumerateHeld(user))
        {
            if (HasComp<PdaComponent>(item) && HasComp<RemoteStoreComponent>(item))
                return item;
        }

        return null;
    }
}

public enum AddUplinkResult
{
    Pda,
    Implant,
    Failure,
}
