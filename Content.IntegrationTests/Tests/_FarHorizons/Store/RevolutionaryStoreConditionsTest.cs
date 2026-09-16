using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Implants;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Store.Conditions;
using Content.Shared.Store.Events;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._FarHorizons.Store;

[TestFixture]
public sealed class RevolutionaryStoreConditionsTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = """
        - type: listing
          id: TestSharedRevolutionaryStock
          cost:
            Telebond: 0
          categories:
          - RevUplinkPassive
          conditions:
          - !type:StockLimitedListingCondition
            stockLimit: 2
        - type: listing
          id: TestReservedRevolutionaryStock
          cost:
            Telebond: 0
          categories:
          - RevUplinkPassive
          conditions:
          - !type:StockLimitedListingCondition
            stockLimit: 1
        """;

    [Test]
    public async Task StockLimitedListingIsSharedBetweenHeadRevolutionaries()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var entMan = server.EntMan;

        await server.WaitAssertion(() =>
        {
            var firstHeadRev = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var secondHeadRev = entMan.SpawnEntity("MobHuman", map.GridCoords);
            entMan.AddComponent<HeadRevolutionaryComponent>(firstHeadRev);
            entMan.AddComponent<HeadRevolutionaryComponent>(secondHeadRev);

            var firstUplink = entMan.SpawnEntity("USSPUplinkRadioPresetDebug", map.GridCoords);
            var secondUplink = entMan.SpawnEntity("USSPUplinkRadioPresetDebug", map.GridCoords);
            var firstStore = entMan.GetComponent<StoreComponent>(firstUplink);
            var secondStore = entMan.GetComponent<StoreComponent>(secondUplink);

            var firstListing = GetListing(firstStore);
            var secondListing = GetListing(secondStore);
            Assert.That(GetStock(firstListing), Is.EqualTo(2));
            Assert.That(GetStock(secondListing), Is.EqualTo(2));

            Buy(entMan, firstUplink, firstHeadRev);
            Assert.That(GetStock(firstListing), Is.EqualTo(1));
            Assert.That(GetStock(secondListing), Is.EqualTo(1));
            var purchaserName = entMan.GetComponent<MetaDataComponent>(firstHeadRev).EntityName;
            Assert.That(GetStockCondition(firstListing).LastPurchaser, Is.EqualTo(purchaserName));
            Assert.That(GetStockCondition(secondListing).LastPurchaser, Is.EqualTo(purchaserName));

            var thirdHeadRev = entMan.SpawnEntity("MobHuman", map.GridCoords);
            entMan.AddComponent<HeadRevolutionaryComponent>(thirdHeadRev);
            var thirdUplink = entMan.SpawnEntity("USSPUplinkRadioPresetDebug", map.GridCoords);
            var thirdListing = GetListing(entMan.GetComponent<StoreComponent>(thirdUplink));
            Assert.That(GetStock(thirdListing), Is.EqualTo(1));
            Assert.That(GetStockCondition(thirdListing).LastPurchaser, Is.EqualTo(purchaserName));

            Buy(entMan, secondUplink, secondHeadRev);
            Assert.That(GetStock(firstListing), Is.Zero);
            Assert.That(GetStock(secondListing), Is.Zero);
            Assert.That(GetStock(thirdListing), Is.Zero);

            Buy(entMan, firstUplink, firstHeadRev);
            Assert.That(firstListing.PurchaseAmount, Is.EqualTo(1));
        });
    }

    public sealed class USSPUplinkImplantStoreTest : InteractionTest
    {
        [Test]
        public async Task CarriedRadioReceivesConversion()
        {
            await Server.WaitPost(() =>
            {
                SEntMan.AddComponent<HeadRevolutionaryComponent>(SPlayer);
                var radio = SEntMan.SpawnEntity(
                    "USSPUplinkRadioPresetDebug",
                    SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates);
                var store = SEntMan.GetComponent<StoreComponent>(radio);

                SEntMan.System<USSPUplinkSystem>().AddConversionToAllHeadRevs(
                    SEntMan.System<StoreSystem>(),
                    new Dictionary<EntityUid, EntityUid> { [SPlayer] = radio });

                Assert.That(store.Balance["Conversion"], Is.EqualTo(FixedPoint2.New(1001)));
            });
        }

        [Test]
        public async Task ImplantsForSameHeadRevShareDetachedStore()
        {
            EntityUid firstImplant = default;
            EntityUid secondImplant = default;
            EntityUid thirdImplant = default;
            EntityUid convertedRevolutionary = default;
            EntityUid store = default;

            await Server.WaitPost(() =>
            {
                SEntMan.AddComponent<HeadRevolutionaryComponent>(SPlayer);
                var implantSystem = SEntMan.System<SharedSubdermalImplantSystem>();
                firstImplant = implantSystem.AddImplant(SPlayer, "USSPUplinkImplant")!.Value;
                convertedRevolutionary = SEntMan.SpawnEntity("MobHuman", SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates);
                SEntMan.AddComponent<RevolutionaryComponent>(convertedRevolutionary);
                SEntMan.AddComponent<RevolutionaryConvertedByComponent>(convertedRevolutionary).ConverterUid = SPlayer;
                secondImplant = implantSystem.AddImplant(convertedRevolutionary, "USSPUplinkImplant")!.Value;

                var firstRemote = SEntMan.GetComponent<RemoteStoreComponent>(firstImplant);
                var secondRemote = SEntMan.GetComponent<RemoteStoreComponent>(secondImplant);
                Assert.That(firstRemote.Store, Is.Not.Null);
                Assert.That(firstRemote.Store, Is.EqualTo(secondRemote.Store));
                Assert.That(SEntMan.HasComponent<StoreComponent>(firstImplant), Is.False);
                Assert.That(SEntMan.HasComponent<StoreComponent>(secondImplant), Is.False);

                store = firstRemote.Store!.Value;
                Assert.That(store, Is.Not.EqualTo(firstImplant));
                Assert.That(SEntMan.HasComponent<StoreComponent>(store), Is.True);
                Assert.That(SEntMan.EntityQuery<RemoteStoreComponent>().Count(remote => remote.Store == store), Is.EqualTo(2));

                var open = new OpenUplinkImplantEvent { Performer = SPlayer };
                SEntMan.EventBus.RaiseLocalEvent(firstImplant, open);
                Assert.That(SUiSys.IsUiOpen((firstImplant, null), StoreUiKey.Key, SPlayer), Is.True);
                SUiSys.CloseUi(firstImplant, StoreUiKey.Key);
                SEntMan.QueueDeleteEntity(firstImplant);
            });

            await RunTicks(5);
            await Server.WaitAssertion(() =>
            {
                Assert.That(SEntMan.EntityExists(store), Is.True);
                Assert.That(SEntMan.GetComponent<RemoteStoreComponent>(secondImplant).Store, Is.EqualTo(store));
                SEntMan.QueueDeleteEntity(secondImplant);
            });

            await RunTicks(5);
            await Server.WaitPost(() =>
            {
                Assert.That(SEntMan.EntityExists(store), Is.True);

                var implantSystem = SEntMan.System<SharedSubdermalImplantSystem>();
                thirdImplant = implantSystem.AddImplant(SPlayer, "USSPUplinkImplant")!.Value;
                Assert.That(SEntMan.GetComponent<RemoteStoreComponent>(thirdImplant).Store, Is.EqualTo(store));
                SEntMan.QueueDeleteEntity(thirdImplant);
            });
        }
    }

    [Test]
    public async Task StockLimitedListingReservesTheLastItem()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var entMan = server.EntMan;

        await server.WaitAssertion(() =>
        {
            var firstHeadRev = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var secondHeadRev = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var firstUplink = entMan.SpawnEntity("USSPUplinkRadioPresetDebug", map.GridCoords);
            var secondUplink = entMan.SpawnEntity("USSPUplinkRadioPresetDebug", map.GridCoords);
            var firstListing = GetListing(entMan.GetComponent<StoreComponent>(firstUplink), "TestReservedRevolutionaryStock");

            var firstAttempt = new StorePurchaseAttemptEvent("TestReservedRevolutionaryStock", firstUplink, firstHeadRev);
            entMan.EventBus.RaiseLocalEvent(firstUplink, ref firstAttempt);
            Assert.That(firstAttempt.Cancel, Is.False);
            Assert.That(GetStock(firstListing), Is.Zero);

            var secondAttempt = new StorePurchaseAttemptEvent("TestReservedRevolutionaryStock", secondUplink, secondHeadRev);
            entMan.EventBus.RaiseLocalEvent(secondUplink, ref secondAttempt);
            Assert.That(secondAttempt.Cancel, Is.True);

            var finish = new StoreBuyFinishedEvent(firstUplink, firstListing, firstHeadRev);
            entMan.EventBus.RaiseLocalEvent(firstUplink, ref finish);
        });
    }

    [Test]
    public async Task RevRiftListingTracksSupplyRiftLifecycle()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var entMan = server.EntMan;

        await server.WaitAssertion(() =>
        {
            var uplink = entMan.SpawnEntity("USSPUplinkRadioPresetDebug", map.GridCoords);
            var listing = entMan.GetComponent<StoreComponent>(uplink).FullListingsCatalog
                .Single(x => x.ID == "RevSupplyRiftListing");
            var condition = listing.Conditions!.OfType<RevRiftListingCondition>().Single();

            Assert.That(condition.Condition(default), Is.True);

            var rift = entMan.SpawnEntity("RevSupplyRift", map.GridCoords);
            Assert.That(condition.Condition(default), Is.False);

            entMan.RemoveComponent<RevSupplyRiftComponent>(rift);
            Assert.That(condition.Condition(default), Is.False);
            Assert.That(condition.RiftDestroyed, Is.True);
        });
    }

    private static ListingDataWithCostModifiers GetListing(StoreComponent store)
    {
        return GetListing(store, "TestSharedRevolutionaryStock");
    }

    private static ListingDataWithCostModifiers GetListing(StoreComponent store, string listingId)
    {
        return store.FullListingsCatalog.Single(x => x.ID == listingId);
    }

    private static int GetStock(ListingData listing)
    {
        var condition = GetStockCondition(listing);
        condition.Condition(default);
        return condition.CurrentStock;
    }

    private static StockLimitedListingCondition GetStockCondition(ListingData listing)
    {
        return listing.Conditions!.OfType<StockLimitedListingCondition>().Single();
    }

    private static void Buy(IEntityManager entMan, EntityUid store, EntityUid buyer)
    {
        var ev = new StoreBuyListingMessage("TestSharedRevolutionaryStock", null) { Actor = buyer };
        entMan.EventBus.RaiseLocalEvent(store, ev);
    }
}
