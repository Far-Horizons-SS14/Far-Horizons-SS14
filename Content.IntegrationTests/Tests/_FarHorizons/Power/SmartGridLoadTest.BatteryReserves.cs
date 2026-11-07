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
    public async Task BatteryChargeHysteresisDelaysAvailabilityRecovery()
    {
        var batterySystem = SEntMan.System<BatterySystem>();
        ApcLoadScenario scenario = default;

        await Server.WaitAssertion(() =>
        {
            scenario = CreateApcLoadScenario(load: 4);
            batterySystem.SetCharge(
                (scenario.Apc, SEntMan.GetComponent<BatteryComponent>(scenario.Apc)),
                8_100f);
        });

        await RunSeconds(17);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();

            var overrideApplied = smartGrid.SetAvailabilityOverride(scenario.Apc, SmartGridPriority.Lighting);
            Assert.That(overrideApplied, Is.True);

            var overrideCleared = smartGrid.SetAvailabilityOverride(scenario.Apc, SmartGridPriority.Lighting);
            Assert.That(overrideCleared, Is.True);
        });

        await RunSeconds(2);

        await Server.WaitAssertion(() =>
        {
            var state = SEntMan.System<SmartGridSystem>().GetAvailabilityState(scenario.Apc);

            Assert.Multiple(() =>
            {
                Assert.That(state.AvailabilityOverride, Is.Null);
                Assert.That(state.EffectiveAvailability, Is.EqualTo(SmartGridPriority.Lighting));
            });
        });
    }

    [Test]
    public async Task PrimaryBatteryChargingRetainsAvailabilityBelowReserve()
    {
        var batterySystem = SEntMan.System<BatterySystem>();
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid smesUid = default;
        EntityUid consumerUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
            mapSystem.SetTile(grid, new Vector2i(0, 1), new Tile(1));

            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 1));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 1));

            var generator = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(0, 0));
            smesUid = SEntMan.SpawnEntity("SmartGridSmesDummy", grid.Owner.ToCoordinates(0, 1));
            consumerUid = SEntMan.SpawnEntity("SmartGridDirectGeneralConsumerDummy", grid.Owner.ToCoordinates(0, 1));

            var supplier = SEntMan.GetComponent<PowerSupplierComponent>(generator);
            supplier.MaxSupply = 200;
            supplier.SupplyRampTolerance = 200;
            batterySystem.SetCharge((smesUid, SEntMan.GetComponent<BatteryComponent>(smesUid)), 7_000f);
            SEntMan.GetComponent<PowerConsumerComponent>(consumerUid).DrawRate = 20;
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var battery = SEntMan.GetComponent<PowerNetworkBatteryComponent>(smesUid);
            var state = smartGrid.GetAvailabilityState(smesUid);

            Assert.Multiple(() =>
            {
                Assert.That(smartGrid.HasUpstreamBatteryOutput(smesUid), Is.False);
                Assert.That(battery.CurrentReceiving, Is.GreaterThan(battery.CurrentSupply));
                Assert.That(state.LocalAvailability, Is.EqualTo(SmartGridPriority.General));
                Assert.That(SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid).Shed, Is.False);
            });
        });
    }

    [Test]
    public async Task PrimaryBatteryThresholdToggleSeedsAutomaticAvailability()
    {
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid smesUid = default;
        EntityUid consumerUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));

            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 0));
            smesUid = SEntMan.SpawnEntity("SmartGridSmesDummy", grid.Owner.ToCoordinates(0, 0));
            consumerUid = SEntMan.SpawnEntity("SmartGridDirectGeneralConsumerDummy", grid.Owner.ToCoordinates(0, 0));
        });

        await RunSeconds(1);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();

            Assert.That(smartGrid.HasUpstreamBatteryOutput(smesUid), Is.False);

            var firstToggleSucceeded = smartGrid.ToggleBatteryAvailability(smesUid, SmartGridPriority.General);
            Assert.That(firstToggleSucceeded, Is.True);
            Assert.That(
                smartGrid.GetAvailabilityState(smesUid).LocalAvailability,
                Is.EqualTo(SmartGridPriority.Lighting));

            var secondToggleSucceeded = smartGrid.ToggleBatteryAvailability(smesUid, SmartGridPriority.Lighting);
            Assert.That(secondToggleSucceeded, Is.True);
            Assert.That(
                smartGrid.GetAvailabilityState(smesUid).LocalAvailability,
                Is.EqualTo(SmartGridPriority.ShuttleOperations));

            var thirdToggleSucceeded = smartGrid.ToggleBatteryAvailability(smesUid, SmartGridPriority.Lighting);
            Assert.That(thirdToggleSucceeded, Is.True);
            Assert.That(
                smartGrid.GetAvailabilityState(smesUid).LocalAvailability,
                Is.EqualTo(SmartGridPriority.Lighting));

            var fourthToggleSucceeded = smartGrid.ToggleBatteryAvailability(smesUid, SmartGridPriority.General);
            Assert.That(fourthToggleSucceeded, Is.True);
            Assert.That(
                smartGrid.GetAvailabilityState(smesUid).LocalAvailability,
                Is.EqualTo(SmartGridPriority.General));

            var tripped = smartGrid.ToggleBatteryAvailability(smesUid, SmartGridPriority.General);
            Assert.That(tripped, Is.True);
            smartGrid.UpdateAfterPowerSolver();

            var trippedState = smartGrid.GetAvailabilityState(smesUid);
            var trippedConsumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);
            Assert.Multiple(() =>
            {
                Assert.That(trippedState.LocalAvailability, Is.EqualTo(SmartGridPriority.Lighting));
                Assert.That(trippedState.EffectiveAvailability, Is.EqualTo(SmartGridPriority.Lighting));
                Assert.That(smartGrid.GetAvailability(trippedConsumer.Net!), Is.EqualTo(SmartGridPriority.Lighting));
                Assert.That(SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid).Shed, Is.True);
            });
        });

        await RunSeconds(17);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var state = smartGrid.GetAvailabilityState(smesUid);
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);

            Assert.Multiple(() =>
            {
                Assert.That(state.LocalAvailability, Is.EqualTo(SmartGridPriority.General));
                Assert.That(state.EffectiveAvailability, Is.EqualTo(SmartGridPriority.General));
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(SmartGridPriority.General));
                Assert.That(SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid).Shed, Is.False);
            });
        });
    }

    [Test]
    public async Task UpstreamBatteryReservePropagatesToChargedApc()
    {
        var batterySystem = SEntMan.System<BatterySystem>();
        var extensionCables = SEntMan.System<ExtensionCableSystem>();
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid apcUid = default;
        Entity<ApcPowerReceiverComponent> general = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            const int range = 5;

            for (var i = 0; i < range; i++)
            {
                mapSystem.SetTile(grid, new Vector2i(0, i), new Tile(1));
            }

            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 1));

            var relay = SEntMan.SpawnEntity("SmartGridRelayDummy", grid.Owner.ToCoordinates(0, 0));
            apcUid = SEntMan.SpawnEntity("SmartGridApcDummy", grid.Owner.ToCoordinates(0, 1));
            var extension = SEntMan.SpawnEntity("CableApcExtension", grid.Owner.ToCoordinates(0, 1));
            var lifeSupportUid = SEntMan.SpawnEntity("SmartGridApcReceiverDummy", grid.Owner.ToCoordinates(0, 3));
            var generalUid = SEntMan.SpawnEntity("SmartGridGeneralReceiverDummy", grid.Owner.ToCoordinates(0, 4));

            general = (generalUid, SEntMan.GetComponent<ApcPowerReceiverComponent>(generalUid));
            SEntMan.GetComponent<ApcPowerReceiverComponent>(lifeSupportUid).Load = 10;
            general.Comp.Load = 10;
            batterySystem.SetCharge((relay, SEntMan.GetComponent<BatteryComponent>(relay)), 7_000f);
            batterySystem.SetCharge((apcUid, SEntMan.GetComponent<BatteryComponent>(apcUid)), 9_000f);

            extensionCables.SetProviderTransferRange(extension, range);
            extensionCables.SetReceiverReceptionRange(lifeSupportUid, range);
            extensionCables.SetReceiverReceptionRange(generalUid, range);
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var state = smartGrid.GetAvailabilityState(apcUid);
            var generalLoad = SEntMan.GetComponent<SmartGridLoadComponent>(general);

            Assert.Multiple(() =>
            {
                Assert.That(state.InputAvailability, Is.EqualTo(SmartGridPriority.Lighting));
                Assert.That(state.EffectiveAvailability, Is.EqualTo(SmartGridPriority.Lighting));
                Assert.That(generalLoad.Shed, Is.True);
                Assert.That(smartGrid.HasUpstreamBatteryOutput(apcUid), Is.True);
            });
        });
    }

    [Test]
    public async Task UpstreamBatteryOutputBreakerControlsReserveMode()
    {
        var batterySystem = SEntMan.System<BatterySystem>();
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid relayUid = default;
        EntityUid apcUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
            mapSystem.SetTile(grid, new Vector2i(0, 1), new Tile(1));

            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 1));
            relayUid = SEntMan.SpawnEntity("SmartGridRelayDummy", grid.Owner.ToCoordinates(0, 0));
            apcUid = SEntMan.SpawnEntity("SmartGridApcDummy", grid.Owner.ToCoordinates(0, 1));
            batterySystem.SetCharge((relayUid, SEntMan.GetComponent<BatteryComponent>(relayUid)), 0f);
        });

        await RunSeconds(1);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var relayBattery = SEntMan.GetComponent<PowerNetworkBatteryComponent>(relayUid);

            Assert.Multiple(() =>
            {
                Assert.That(relayBattery.CanDischarge, Is.True);
                Assert.That(smartGrid.HasUpstreamBatteryOutput(apcUid), Is.True);
            });

            relayBattery.CanDischarge = false;

            Assert.That(smartGrid.HasUpstreamBatteryOutput(apcUid), Is.False);
        });
    }

    private static readonly TestCaseData[] CappedInputBatteryReserveCases =
    {
        CreateBatteryReserveCase(
            name: "DepletedSmesPropagatesCappedInputAvailability",
            batteryCharge: 7_000f,
            secondBatteryCharge: null,
            generatorEnabled: true,
            expectedInputAvailability: SmartGridPriority.Lighting,
            expectedAvailability: SmartGridPriority.Lighting),
        CreateBatteryReserveCase(
            name: "ChargedSmesSustainsAvailabilityWithDisabledInputSuppliers",
            batteryCharge: 10_000f,
            secondBatteryCharge: null,
            generatorEnabled: false,
            expectedInputAvailability: null,
            expectedAvailability: SmartGridPriority.General),
        CreateBatteryReserveCase(
            name: "ChargedSmesSustainsSharedOutputWithDepletedSmes",
            batteryCharge: 9_000f,
            secondBatteryCharge: 6_000f,
            generatorEnabled: true,
            expectedInputAvailability: SmartGridPriority.Lighting,
            expectedAvailability: SmartGridPriority.General),
    };

    [TestCaseSource(nameof(CappedInputBatteryReserveCases))]
    public async Task BatteryReserveControlsCappedInputAvailability(
        float batteryCharge,
        float? secondBatteryCharge,
        bool generatorEnabled,
        SmartGridPriority? expectedInputAvailability,
        SmartGridPriority expectedAvailability)
    {
        var batterySystem = SEntMan.System<BatterySystem>();
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid smesUid = default;
        EntityUid consumerUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
            mapSystem.SetTile(grid, new Vector2i(0, 1), new Tile(1));

            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 1));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 1));

            var generator = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(0, 0));
            var disabledGenerator = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(0, 0));
            smesUid = SEntMan.SpawnEntity("SmartGridSmesDummy", grid.Owner.ToCoordinates(0, 1));
            consumerUid = SEntMan.SpawnEntity("SmartGridDirectGeneralConsumerDummy", grid.Owner.ToCoordinates(0, 1));

            var supplier = SEntMan.GetComponent<PowerSupplierComponent>(generator);
            supplier.MaxSupply = 10;
            supplier.SupplyRampTolerance = 10;
            supplier.Enabled = generatorEnabled;
            SEntMan.GetComponent<PowerSupplierComponent>(disabledGenerator).Enabled = false;

            SEntMan.GetComponent<PowerConsumerComponent>(consumerUid).DrawRate = 20;
            batterySystem.SetCharge((smesUid, SEntMan.GetComponent<BatteryComponent>(smesUid)), batteryCharge);
            if (secondBatteryCharge is { } secondCharge)
            {
                var secondSmes = SEntMan.SpawnEntity("SmartGridSmesDummy", grid.Owner.ToCoordinates(0, 1));
                batterySystem.SetCharge((secondSmes, SEntMan.GetComponent<BatteryComponent>(secondSmes)), secondCharge);
            }
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);
            var state = smartGrid.GetAvailabilityState(smesUid);

            Assert.Multiple(() =>
            {
                Assert.That(state.InputAvailability, Is.EqualTo(expectedInputAvailability));
                Assert.That(state.EffectiveAvailability, Is.EqualTo(expectedAvailability));
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(expectedAvailability));
                Assert.That(smartGrid.HasUpstreamBatteryOutput(smesUid), Is.False);
            });
        });
    }

    [TestCase(7_000f, 4, SmartGridPriority.Lighting, TestName = "DisconnectedSmesShedsAtItsOwnReserveThreshold")]
    [TestCase(0f, 10, SmartGridPriority.EssentialSystems, TestName = "EmptyDisconnectedSmesEmitsEssentialSystems")]
    public async Task DisconnectedSmesUsesItsOwnAvailability(
        float charge,
        int seconds,
        SmartGridPriority expectedAvailability)
    {
        var batterySystem = SEntMan.System<BatterySystem>();
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid smesUid = default;
        EntityUid consumerUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));

            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 0));
            smesUid = SEntMan.SpawnEntity("SmartGridSmesDummy", grid.Owner.ToCoordinates(0, 0));
            consumerUid = SEntMan.SpawnEntity("SmartGridDirectGeneralConsumerDummy", grid.Owner.ToCoordinates(0, 0));

            SEntMan.GetComponent<PowerConsumerComponent>(consumerUid).DrawRate = 20;
            batterySystem.SetCharge((smesUid, SEntMan.GetComponent<BatteryComponent>(smesUid)), charge);
        });

        await RunSeconds(seconds);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);
            var state = smartGrid.GetAvailabilityState(smesUid);

            Assert.Multiple(() =>
            {
                Assert.That(state.InputAvailability, Is.Null);
                Assert.That(state.EffectiveAvailability, Is.EqualTo(expectedAvailability));
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(expectedAvailability));
            });
        });
    }

    [Test]
    public async Task PrimaryBatteriesTrackReserveBreakersIndependently()
    {
        var batterySystem = SEntMan.System<BatterySystem>();
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid depletedSmes = default;
        EntityUid chargedSmes = default;
        EntityUid consumerUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));

            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 0));
            depletedSmes = SEntMan.SpawnEntity("SmartGridSmesDummy", grid.Owner.ToCoordinates(0, 0));
            chargedSmes = SEntMan.SpawnEntity("SmartGridSmesDummy", grid.Owner.ToCoordinates(0, 0));
            consumerUid = SEntMan.SpawnEntity("SmartGridDirectGeneralConsumerDummy", grid.Owner.ToCoordinates(0, 0));

            SEntMan.GetComponent<PowerConsumerComponent>(consumerUid).DrawRate = 20;
            batterySystem.SetCharge((depletedSmes, SEntMan.GetComponent<BatteryComponent>(depletedSmes)), 7_000f);
            batterySystem.SetCharge((chargedSmes, SEntMan.GetComponent<BatteryComponent>(chargedSmes)), 10_000f);
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);

            Assert.Multiple(() =>
            {
                Assert.That(
                    smartGrid.GetAvailabilityState(depletedSmes).LocalAvailability,
                    Is.EqualTo(SmartGridPriority.Lighting));
                Assert.That(
                    smartGrid.GetAvailabilityState(chargedSmes).LocalAvailability,
                    Is.EqualTo(SmartGridPriority.General));
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(SmartGridPriority.General));
                Assert.That(SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid).Shed, Is.False);
            });
        });
    }

    [Test]
    public async Task PrimaryBatteriesPreserveAvailabilityWhileInputIsCapped()
    {
        var batterySystem = SEntMan.System<BatterySystem>();
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid firstSmes = default;
        EntityUid secondSmes = default;
        EntityUid consumerUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
            mapSystem.SetTile(grid, new Vector2i(0, 1), new Tile(1));

            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 1));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 1));

            var generator = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(0, 0));
            firstSmes = SEntMan.SpawnEntity("SmartGridSmesDummy", grid.Owner.ToCoordinates(0, 1));
            secondSmes = SEntMan.SpawnEntity("SmartGridSmesDummy", grid.Owner.ToCoordinates(0, 1));
            consumerUid = SEntMan.SpawnEntity("SmartGridDirectGeneralConsumerDummy", grid.Owner.ToCoordinates(0, 1));

            var supplier = SEntMan.GetComponent<PowerSupplierComponent>(generator);
            supplier.MaxSupply = 10;
            supplier.SupplyRampTolerance = 10;
            SEntMan.GetComponent<PowerConsumerComponent>(consumerUid).DrawRate = 200;
            batterySystem.SetCharge((firstSmes, SEntMan.GetComponent<BatteryComponent>(firstSmes)), 8_500f);
            batterySystem.SetCharge((secondSmes, SEntMan.GetComponent<BatteryComponent>(secondSmes)), 8_500f);
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();

            Assert.Multiple(() =>
            {
                Assert.That(
                    smartGrid.GetAvailabilityState(firstSmes).InputAvailability,
                    Is.LessThan(SmartGridPriority.General));
                Assert.That(
                    smartGrid.GetAvailabilityState(firstSmes).LocalAvailability,
                    Is.EqualTo(SmartGridPriority.General));
                Assert.That(
                    smartGrid.GetAvailabilityState(secondSmes).LocalAvailability,
                    Is.EqualTo(SmartGridPriority.General));
            });

            SEntMan.GetComponent<PowerConsumerComponent>(consumerUid).DrawRate = 20;
        });

        await RunSeconds(2);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);

            Assert.Multiple(() =>
            {
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(SmartGridPriority.General));
                Assert.That(SEntMan.GetComponent<SmartGridLoadComponent>(consumerUid).Shed, Is.False);
                Assert.That(
                    smartGrid.GetAvailabilityState(firstSmes).LocalAvailability,
                    Is.EqualTo(SmartGridPriority.General));
                Assert.That(
                    smartGrid.GetAvailabilityState(secondSmes).LocalAvailability,
                    Is.EqualTo(SmartGridPriority.General));
            });
        });
    }

    [Test]
    public async Task SmesWithHigherInputAvailabilitySustainsSharedOutput()
    {
        var batterySystem = SEntMan.System<BatterySystem>();
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid constrainedSmes = default;
        EntityUid healthySmes = default;
        EntityUid consumerUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    mapSystem.SetTile(grid, new Vector2i(x, y), new Tile(1));
                }
            }

            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(0, 1));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(2, 0));
            SEntMan.SpawnEntity("CableHV", grid.Owner.ToCoordinates(2, 1));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(0, 1));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(1, 1));
            SEntMan.SpawnEntity("CableMV", grid.Owner.ToCoordinates(2, 1));

            var constrainedGenerator = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(0, 0));
            var healthyGenerator = SEntMan.SpawnEntity("SmartGridGeneratorDummy", grid.Owner.ToCoordinates(2, 0));
            constrainedSmes = SEntMan.SpawnEntity("SmartGridSmesDummy", grid.Owner.ToCoordinates(0, 1));
            healthySmes = SEntMan.SpawnEntity("SmartGridSmesDummy", grid.Owner.ToCoordinates(2, 1));
            consumerUid = SEntMan.SpawnEntity("SmartGridDirectGeneralConsumerDummy", grid.Owner.ToCoordinates(1, 1));

            var supplier = SEntMan.GetComponent<PowerSupplierComponent>(constrainedGenerator);
            supplier.MaxSupply = 10;
            supplier.SupplyRampTolerance = 10;
            var healthySupplier = SEntMan.GetComponent<PowerSupplierComponent>(healthyGenerator);
            healthySupplier.MaxSupply = 100;
            healthySupplier.SupplyRampTolerance = 100;

            SEntMan.GetComponent<PowerConsumerComponent>(consumerUid).DrawRate = 20;
            batterySystem.SetCharge((constrainedSmes, SEntMan.GetComponent<BatteryComponent>(constrainedSmes)), 6_000f);
            batterySystem.SetCharge((healthySmes, SEntMan.GetComponent<BatteryComponent>(healthySmes)), 10_000f);
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var consumer = SEntMan.GetComponent<PowerConsumerComponent>(consumerUid);

            Assert.Multiple(() =>
            {
                Assert.That(
                    smartGrid.GetAvailabilityState(constrainedSmes).InputAvailability,
                    Is.EqualTo(SmartGridPriority.Lighting));
                Assert.That(
                    smartGrid.GetAvailabilityState(healthySmes).InputAvailability,
                    Is.EqualTo(SmartGridPriority.General));
                Assert.That(smartGrid.GetAvailability(consumer.Net!), Is.EqualTo(SmartGridPriority.General));
            });
        });
    }

    private static TestCaseData CreateBatteryReserveCase(
        string name,
        float batteryCharge,
        float? secondBatteryCharge,
        bool generatorEnabled,
        SmartGridPriority? expectedInputAvailability,
        SmartGridPriority expectedAvailability)
    {
        return new TestCaseData(
                batteryCharge,
                secondBatteryCharge,
                generatorEnabled,
                expectedInputAvailability,
                expectedAvailability)
            .SetName(name);
    }
}
