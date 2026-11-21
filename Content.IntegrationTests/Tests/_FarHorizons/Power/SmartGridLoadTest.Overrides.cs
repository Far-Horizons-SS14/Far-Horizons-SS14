using Content.Server._FarHorizons.Power.SmartGrid;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Content.Shared.Coordinates;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._FarHorizons.Power;

public sealed partial class SmartGridLoadTest
{
    [Test]
    public async Task GeneratorSpoolUpMaintainsGeneralAvailability()
    {
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid generatorUid = default;
        EntityUid consumerUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
            mapSystem.SetTile(grid, new Vector2i(0, 1), new Tile(1));

            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 1));
            generatorUid = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(0, 0));
            consumerUid = SEntMan.SpawnEntity(
                "SmartGridDirectHighGeneralConsumerDummy",
                grid.Owner.ToCoordinates(0, 1));

            var supplier = SEntMan.GetComponent<PowerSupplierComponent>(generatorUid);
            supplier.MaxSupply = 100;
            supplier.SupplyRampTolerance = 0;
            supplier.SupplyRampRate = 1;
        });

        await RunSeconds(2);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var supplier = SEntMan.GetComponent<PowerSupplierComponent>(generatorUid);
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);
            var load = SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid);

            Assert.Multiple(() =>
            {
                Assert.That(supplier.CurrentSupply, Is.LessThan(consumer.DrawRate));
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(SmartGridPriority.General));
                Assert.That(load.Shed, Is.False);
            });
        });
    }

    [Test]
    public async Task RelayAvailabilityOverrideSeedsAutomaticAvailability()
    {
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid relayUid = default;
        EntityUid consumerUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            for (var i = 0; i < 3; i++)
            {
                mapSystem.SetTile(grid, new Vector2i(0, i), new Tile(1));
            }

            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 1));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 1));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 2));

            var generator = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(0, 0));
            relayUid = SEntMan.SpawnEntity("SmartGridRelayDummy", grid.Owner.ToCoordinates(0, 1));
            consumerUid = SEntMan.SpawnEntity("SmartGridDirectGeneralConsumerDummy", grid.Owner.ToCoordinates(0, 2));

            var supplier = SEntMan.GetComponent<PowerSupplierComponent>(generator);
            supplier.MaxSupply = 1_000;
            supplier.SupplyRampTolerance = 1_000;

            var relayBattery = SEntMan.GetComponent<PowerNetworkBatteryComponent>(relayUid);
            relayBattery.MaxSupply = 1_000;
            relayBattery.SupplyRampTolerance = 1_000;
        });

        await RunSeconds(2);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);
            var load = SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid);
            var state = smartGrid.GetAvailabilityState(relayUid);

            Assert.Multiple(() =>
            {
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(SmartGridPriority.General));
                Assert.That(load.Shed, Is.False);
                Assert.That(state.InputAvailability, Is.EqualTo(SmartGridPriority.General));
                Assert.That(state.EffectiveAvailability, Is.EqualTo(SmartGridPriority.General));
                Assert.That(state.AvailabilityOverride, Is.Null);
            });

            var overrideApplied = smartGrid.SetAvailabilityOverride(relayUid, SmartGridPriority.Lighting);
            Assert.That(overrideApplied, Is.True);
        });

        await RunSeconds(2);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);
            var load = SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid);
            var state = smartGrid.GetAvailabilityState(relayUid);

            Assert.Multiple(() =>
            {
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(SmartGridPriority.Lighting));
                Assert.That(load.Shed, Is.True);
                Assert.That(state.AvailabilityOverride, Is.EqualTo(SmartGridPriority.Lighting));
            });

            var overrideCleared = smartGrid.SetAvailabilityOverride(relayUid, SmartGridPriority.Lighting);
            Assert.That(overrideCleared, Is.True);
        });

        await RunSeconds(2);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);
            var load = SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid);
            var state = smartGrid.GetAvailabilityState(relayUid);

            Assert.Multiple(() =>
            {
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(SmartGridPriority.Lighting));
                Assert.That(load.Shed, Is.True);
                Assert.That(state.AvailabilityOverride, Is.Null);
            });
        });
    }

    [Test]
    public async Task GeneratorOverrideRestoresAutomaticAvailability()
    {
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid generatorUid = default;
        EntityUid consumerUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            for (var i = 0; i < 2; i++)
            {
                mapSystem.SetTile(grid, new Vector2i(0, i), new Tile(1));
            }

            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 1));
            generatorUid = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(0, 0));
            consumerUid = SEntMan.SpawnEntity(
                "SmartGridDirectHighGeneralConsumerDummy",
                grid.Owner.ToCoordinates(0, 1));

            var supplier = SEntMan.GetComponent<PowerSupplierComponent>(generatorUid);
            supplier.MaxSupply = 10;
            supplier.SupplyRampTolerance = 10;
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);
            var state = smartGrid.GetAvailabilityState(generatorUid);

            Assert.Multiple(() =>
            {
                Assert.That(state.InputAvailability, Is.Null);
                Assert.That(state.EffectiveAvailability, Is.LessThan(SmartGridPriority.General));
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.LessThan(SmartGridPriority.General));
            });

            var overrideApplied = smartGrid.SetAvailabilityOverride(generatorUid, SmartGridPriority.General);
            Assert.That(overrideApplied, Is.True);
        });

        await RunSeconds(2);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);
            var load = SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid);
            var state = smartGrid.GetAvailabilityState(generatorUid);

            Assert.Multiple(() =>
            {
                Assert.That(state.InputAvailability, Is.Null);
                Assert.That(state.EffectiveAvailability, Is.EqualTo(SmartGridPriority.General));
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(SmartGridPriority.General));
                Assert.That(load.Shed, Is.False);
            });

            var overrideCleared = smartGrid.SetAvailabilityOverride(generatorUid, SmartGridPriority.General);
            Assert.That(overrideCleared, Is.True);
        });

        await RunSeconds(2);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);
            var load = SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid);

            Assert.Multiple(() =>
            {
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.LessThan(SmartGridPriority.General));
                Assert.That(load.Shed, Is.True);
                Assert.That(smartGrid.GetAvailabilityState(generatorUid).AvailabilityOverride, Is.Null);
            });
        });
    }
}
