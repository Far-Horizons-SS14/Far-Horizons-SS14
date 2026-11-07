using Content.Server._FarHorizons.Power.SmartGrid;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Content.Shared.Coordinates;
using Content.Shared.Power.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._FarHorizons.Power;

public sealed partial class SmartGridLoadTest
{
    [Test]
    public async Task CappedSupplierWithoutSmartGridControlShedsLoad()
    {
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid consumerUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
            mapSystem.SetTile(grid, new Vector2i(0, 1), new Tile(1));

            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 1));
            var generator = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(0, 0));
            consumerUid = SEntMan.SpawnEntity(
                "SmartGridDirectHighGeneralConsumerDummy",
                grid.Owner.ToCoordinates(0, 1));

            var supplier = SEntMan.GetComponent<PowerSupplierComponent>(generator);
            supplier.MaxSupply = 10;
            supplier.SupplyRampTolerance = 10;
            SEntMan.RemoveComponent<SmartGridControlComponent>(generator);
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);
            var load = SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid);
            var smartGrid = SEntMan.System<SmartGridSystem>();

            Assert.Multiple(() =>
            {
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.LessThan(SmartGridPriority.General));
                Assert.That(load.Shed, Is.True);
            });
        });
    }

    [Test]
    public async Task RecoveryDoesNotRequireArbitrarySupplyHeadroom()
    {
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid lifeSupportConsumer = default;
        EntityUid generalConsumer = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
            mapSystem.SetTile(grid, new Vector2i(0, 1), new Tile(1));

            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 1));
            var generator = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(0, 0));
            lifeSupportConsumer = SEntMan.SpawnEntity(
                "SmartGridDirectHighGeneralConsumerDummy",
                grid.Owner.ToCoordinates(0, 1));
            generalConsumer = SEntMan.SpawnEntity(
                "SmartGridDirectHighGeneralConsumerDummy",
                grid.Owner.ToCoordinates(0, 1));

            var supplier = SEntMan.GetComponent<PowerSupplierComponent>(generator);
            supplier.MaxSupply = 100;
            supplier.SupplyRampTolerance = 100;
            SEntMan.GetComponent<PowerConsumerComponent>(lifeSupportConsumer).DrawRate = 90;
            SEntMan.GetComponent<SmartGridLoadComponent>(lifeSupportConsumer).Priority =
                SmartGridPriority.LifeSupport;
            SEntMan.GetComponent<PowerConsumerComponent>(generalConsumer).DrawRate = 20;
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(generalConsumer);

            Assert.Multiple(() =>
            {
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.LessThan(SmartGridPriority.General));
                Assert.That(SEntMan.GetComponent<SmartGridLoadComponent>(generalConsumer).Shed, Is.True);
            });

            // Leave a stable ten-percent margin once General is restored.
            SEntMan.GetComponent<PowerConsumerComponent>(lifeSupportConsumer).DrawRate = 70;
        });

        await RunSeconds(16);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(generalConsumer);

            Assert.Multiple(() =>
            {
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(SmartGridPriority.General));
                Assert.That(SEntMan.GetComponent<SmartGridLoadComponent>(generalConsumer).Shed, Is.False);
            });
        });
    }

    [TestCase(4f, 7_000f, TestName = "TierReserveShedsGeneralBeforeLifeSupport")]
    public async Task ConstrainedSourceShedsGeneralBeforeLifeSupport(float load, float charge)
    {
        var batterySystem = SEntMan.System<BatterySystem>();
        ApcLoadScenario scenario = default;

        await Server.WaitAssertion(() =>
        {
            scenario = CreateApcLoadScenario(load);
            batterySystem.SetCharge(
                (scenario.Apc, SEntMan.GetComponent<BatteryComponent>(scenario.Apc)),
                charge);

            Assert.Multiple(() =>
            {
                Assert.That(
                    SEntMan.GetComponent<SmartGridLoadComponent>(scenario.LifeSupport).Priority,
                    Is.EqualTo(SmartGridPriority.LifeSupport));
                Assert.That(
                    SEntMan.GetComponent<SmartGridLoadComponent>(scenario.General).Priority,
                    Is.EqualTo(SmartGridPriority.General));
            });
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var lifeSupportLoad = SEntMan.GetComponent<SmartGridLoadComponent>(scenario.LifeSupport);
            var generalLoad = SEntMan.GetComponent<SmartGridLoadComponent>(scenario.General);
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var hasStatus = smartGrid.TryGetStatus(scenario.LifeSupport.Comp.Provider!.Net, out var status);

            Assert.That(hasStatus, Is.True);
            if (!hasStatus)
                return;

            Assert.Multiple(() =>
            {
                Assert.That(lifeSupportLoad.Shed, Is.False);
                Assert.That(generalLoad.Shed, Is.True);
                Assert.That(scenario.LifeSupport.Comp.NetworkLoad.Enabled, Is.True);
                Assert.That(scenario.General.Comp.NetworkLoad.Enabled, Is.False);
                Assert.That(status.Availability, Is.LessThan(SmartGridPriority.General));
            });
        });
    }

    [Test]
    public async Task UpstreamShortagePropagatesToApcLoads()
    {
        var batterySystem = SEntMan.System<BatterySystem>();
        var extensionCables = SEntMan.System<ExtensionCableSystem>();
        var mapSystem = SEntMan.System<SharedMapSystem>();
        Entity<ApcPowerReceiverComponent> lifeSafety = default!;
        Entity<ApcPowerReceiverComponent> general = default!;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            const int range = 5;

            for (var i = 0; i < range; i++)
            {
                mapSystem.SetTile(grid, new Vector2i(0, i), new Tile(1));
            }

            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 1));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 1));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 2));

            var generator = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(0, 0));
            var relay = SEntMan.SpawnEntity("SmartGridRelayDummy", grid.Owner.ToCoordinates(0, 1));
            SEntMan.SpawnEntity("SmartGridDirectGeneralConsumerDummy", grid.Owner.ToCoordinates(0, 2));
            SEntMan.SpawnEntity("SmartGridApcDummy", grid.Owner.ToCoordinates(0, 2));
            var extension = SEntMan.SpawnEntity("CableApcExtension", grid.Owner.ToCoordinates(0, 2));
            var lifeSafetyUid = SEntMan.SpawnEntity("SmartGridApcReceiverDummy", grid.Owner.ToCoordinates(0, 3));
            var generalUid = SEntMan.SpawnEntity("SmartGridGeneralReceiverDummy", grid.Owner.ToCoordinates(0, 4));

            lifeSafety = (lifeSafetyUid, SEntMan.GetComponent<ApcPowerReceiverComponent>(lifeSafetyUid));
            general = (generalUid, SEntMan.GetComponent<ApcPowerReceiverComponent>(generalUid));
            lifeSafety.Comp.Load = 1;
            general.Comp.Load = 1;

            var supplier = SEntMan.GetComponent<PowerSupplierComponent>(generator);
            supplier.MaxSupply = 10;
            supplier.SupplyRampTolerance = 10;
            batterySystem.SetCharge((relay, SEntMan.GetComponent<BatteryComponent>(relay)), 7_000f);

            extensionCables.SetProviderTransferRange(extension, range);
            extensionCables.SetReceiverReceptionRange(lifeSafetyUid, range);
            extensionCables.SetReceiverReceptionRange(generalUid, range);
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var lifeSafetyLoad = SEntMan.GetComponent<SmartGridLoadComponent>(lifeSafety);
            var generalLoad = SEntMan.GetComponent<SmartGridLoadComponent>(general);

            Assert.Multiple(() =>
            {
                Assert.That(lifeSafetyLoad.Shed, Is.False);
                Assert.That(generalLoad.Shed, Is.True);
                Assert.That(lifeSafety.Comp.NetworkLoad.Enabled, Is.True);
                Assert.That(general.Comp.NetworkLoad.Enabled, Is.False);
            });
        });
    }
}
