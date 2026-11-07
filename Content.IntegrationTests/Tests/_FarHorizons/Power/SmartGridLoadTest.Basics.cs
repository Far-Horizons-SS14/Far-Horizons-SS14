using System.Linq;
using Content.Server._FarHorizons.Power.SmartGrid;
using Content.Server.Power.Components;
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
    public async Task GasPumpsAreLifeSupportLoads()
    {
        await Server.WaitAssertion(() =>
        {
            foreach (var prototype in new[]
                     {
                         "GasPressurePump",
                         "GasPressurePumpAlt1",
                         "GasPressurePumpAlt2",
                         "GasVolumePump",
                         "GasVolumePumpAlt1",
                         "GasVolumePumpAlt2",
                     })
            {
                var pump = SEntMan.SpawnEntity(prototype, MapCoordinates.Nullspace);
                var load = SEntMan.GetComponent<SmartGridLoadComponent>(pump);

                Assert.That(load.Priority, Is.EqualTo(SmartGridPriority.LifeSupport), prototype);
            }
        });
    }

    [Test]
    public async Task SheddingPreservesManualPowerState()
    {
        await Server.WaitAssertion(() =>
        {
            var loadSystem = SEntMan.System<SmartGridLoadSystem>();
            var uid = SEntMan.SpawnEntity("SmartGridApcReceiverDummy", MapCoordinates.Nullspace);
            var receiver = SEntMan.GetComponent<ApcPowerReceiverComponent>(uid);
            var smartGridLoad = SEntMan.GetComponent<SmartGridLoadComponent>(uid);

            Assert.That(smartGridLoad.Priority, Is.EqualTo(SmartGridPriority.LifeSupport));
            Assert.That(receiver.NetworkLoad.Enabled, Is.True);

            receiver.PowerDisabled = true;
            Assert.That(receiver.NetworkLoad.Enabled, Is.False);

            var shedApplied = loadSystem.SetShed(uid, true, smartGridLoad);
            Assert.That(shedApplied, Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(receiver.PowerDisabled, Is.True);
                Assert.That(receiver.SmartGridDisabled, Is.True);
                Assert.That(receiver.NetworkLoad.Enabled, Is.False);
            });

            var shedCleared = loadSystem.SetShed(uid, false, smartGridLoad);
            Assert.That(shedCleared, Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(receiver.PowerDisabled, Is.True);
                Assert.That(receiver.SmartGridDisabled, Is.False);
                Assert.That(receiver.NetworkLoad.Enabled, Is.False);
            });

            var directUid = SEntMan.SpawnEntity("SmartGridDirectGeneralConsumerDummy", MapCoordinates.Nullspace);
            var directConsumer = SEntMan.GetComponent<PowerConsumerComponent>(directUid);
            var directLoad = SEntMan.GetComponent<SmartGridLoadComponent>(directUid);
            directConsumer.NetworkLoad.Enabled = false;

            var directShedApplied = loadSystem.SetShed(directUid, true, directLoad);
            Assert.That(directShedApplied, Is.True);
            Assert.That(directConsumer.NetworkLoad.Enabled, Is.False);

            var directShedCleared = loadSystem.SetShed(directUid, false, directLoad);
            Assert.That(directShedCleared, Is.True);
            Assert.That(directConsumer.NetworkLoad.Enabled, Is.False);
        });
    }

    [Test]
    public async Task ApcExternalPowerUsesLoadLimit()
    {
        var mapSystem = SEntMan.System<SharedMapSystem>();
        EntityUid apcUid = default;

        await Server.WaitAssertion(() =>
        {
            var map = mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
            apcUid = SEntMan.SpawnEntity("SmartGridApcDummy", grid.Owner.ToCoordinates(0, 0));
        });

        await Server.WaitRunTicks(1);

        await Server.WaitAssertion(() =>
        {
            var apc = SEntMan.GetComponent<ApcComponent>(apcUid);
            var battery = SEntMan.GetComponent<PowerNetworkBatteryComponent>(apcUid);
            var smartGrid = SEntMan.System<SmartGridSystem>();

            Assert.That(apc.Net, Is.Not.Null);

            battery.CurrentSupply = 11_000f;
            battery.CurrentReceiving = 11_000f;
            var externalSource = smartGrid.GetImmediateSources(apc.Net!).Single();

            battery.CurrentReceiving = 0f;
            var batterySource = smartGrid.GetImmediateSources(apc.Net!).Single();

            Assert.Multiple(() =>
            {
                Assert.That(externalSource.MaximumOutput, Is.EqualTo(apc.MaxLoad));
                Assert.That(externalSource.Health, Is.EqualTo(SmartGridSourceHealth.Healthy));
                Assert.That(batterySource.MaximumOutput, Is.EqualTo(battery.MaxSupply));
                Assert.That(batterySource.Health, Is.EqualTo(SmartGridSourceHealth.OutputCapped));
            });
        });
    }

    [Test]
    public async Task FreestandingApcUsesLocalAvailabilityWithoutInputSource()
    {
        ApcLoadScenario scenario = default;

        await Server.WaitAssertion(() =>
        {
            scenario = CreateApcLoadScenario(load: 4);
        });

        await RunSeconds(4);

        await Server.WaitAssertion(() =>
        {
            var smartGrid = SEntMan.System<SmartGridSystem>();
            var state = smartGrid.GetAvailabilityState(scenario.Apc);

            Assert.Multiple(() =>
            {
                Assert.That(state.InputAvailability, Is.Null);
                Assert.That(state.EffectiveAvailability, Is.EqualTo(SmartGridPriority.General));
                Assert.That(SEntMan.GetComponent<SmartGridLoadComponent>(scenario.General).Shed, Is.False);
            });
        });
    }
}
