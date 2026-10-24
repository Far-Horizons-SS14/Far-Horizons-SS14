using Content.IntegrationTests.Fixtures;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Coordinates;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._FarHorizons.Power;

[TestFixture]
public sealed partial class SmartGridLoadTest : GameTest
{
    public override PoolSettings PoolSettings => PsDisconnected;

    [TestPrototypes]
    private const string Prototypes = """
        - type: entity
          id: SmartGridApcReceiverDummy
          components:
          - type: ApcPowerReceiver
          - type: SmartGridLoad
            priority: LifeSupport
          - type: ExtensionCableReceiver
          - type: Transform
            anchored: true

        - type: entity
          id: SmartGridApcDummy
          components:
          - type: Battery
            netsync: false
            maxCharge: 10000
            startingCharge: 10000
          - type: PowerNetworkBattery
            maxSupply: 10
            supplyRampTolerance: 10
          - type: BatteryCharger
            voltage: Medium
          - type: BatteryDischarger
            voltage: Apc
          - type: Apc
            voltage: Apc
          - type: SmartGridControl
          - type: NodeContainer
            nodes:
              input:
                !type:CableDeviceNode
                nodeGroupID: MVPower
              output:
                !type:CableDeviceNode
                nodeGroupID: Apc
          - type: Transform
            anchored: true

        - type: entity
          id: SmartGridGeneratorDummy
          components:
          - type: NodeContainer
            nodes:
              output:
                !type:CableDeviceNode
                nodeGroupID: HVPower
          - type: PowerSupplier
          - type: SmartGridControl
          - type: Transform
            anchored: true

        - type: entity
          id: SmartGridRelayDummy
          components:
          - type: Battery
            netsync: false
            maxCharge: 10000
            startingCharge: 10000
          - type: PowerNetworkBattery
            maxChargeRate: 100
            maxSupply: 100
            supplyRampTolerance: 100
            supplyRampRate: 100
          - type: BatteryCharger
            voltage: High
          - type: BatteryDischarger
            voltage: Medium
          - type: SmartGridControl
          - type: NodeContainer
            nodes:
              input:
                !type:CableDeviceNode
                nodeGroupID: HVPower
              output:
                !type:CableDeviceNode
                nodeGroupID: MVPower
          - type: Transform
            anchored: true

        - type: entity
          parent: SmartGridRelayDummy
          id: SmartGridSmesDummy
          components:
          - type: Smes

        - type: entity
          id: SmartGridDirectGeneralConsumerDummy
          components:
          - type: PowerConsumer
            voltage: Medium
            drawRate: 100
          - type: SmartGridLoad
            priority: General
          - type: NodeContainer
            nodes:
              input:
                !type:CableDeviceNode
                nodeGroupID: MVPower
          - type: Transform
            anchored: true

        - type: entity
          id: SmartGridDirectHighGeneralConsumerDummy
          components:
          - type: PowerConsumer
            voltage: High
            drawRate: 100
          - type: SmartGridLoad
            priority: General
          - type: NodeContainer
            nodes:
              input:
                !type:CableDeviceNode
                nodeGroupID: HVPower
          - type: Transform
            anchored: true

        - type: entity
          id: SmartGridGeneralReceiverDummy
          components:
          - type: ApcPowerReceiver
            powerLoad: 100
          - type: ExtensionCableReceiver
          - type: Transform
            anchored: true
        """;

    private ApcLoadScenario CreateApcLoadScenario(float load)
    {
        var mapSystem = SEntMan.System<SharedMapSystem>();
        var extensionCables = SEntMan.System<ExtensionCableSystem>();
        mapSystem.CreateMap(out var mapId);
        var grid = mapSystem.CreateGridEntity(mapId);
        const int range = 5;

        for (var i = 0; i < range; i++)
        {
            mapSystem.SetTile(grid, new Vector2i(0, i), new Tile(1));
        }

        var apc = SEntMan.SpawnEntity("SmartGridApcDummy", grid.Owner.ToCoordinates(0, 0));
        var extension = SEntMan.SpawnEntity("CableApcExtension", grid.Owner.ToCoordinates(0, 0));
        var lifeSupportUid = SEntMan.SpawnEntity("SmartGridApcReceiverDummy", grid.Owner.ToCoordinates(0, 3));
        var generalUid = SEntMan.SpawnEntity("SmartGridGeneralReceiverDummy", grid.Owner.ToCoordinates(0, 4));
        Entity<ApcPowerReceiverComponent> lifeSupport =
            (lifeSupportUid, SEntMan.GetComponent<ApcPowerReceiverComponent>(lifeSupportUid));
        Entity<ApcPowerReceiverComponent> general =
            (generalUid, SEntMan.GetComponent<ApcPowerReceiverComponent>(generalUid));

        lifeSupport.Comp.Load = load;
        general.Comp.Load = load;
        extensionCables.SetProviderTransferRange(extension, range);
        extensionCables.SetReceiverReceptionRange(lifeSupportUid, range);
        extensionCables.SetReceiverReceptionRange(generalUid, range);

        return new ApcLoadScenario(apc, lifeSupport, general);
    }

    private readonly record struct ApcLoadScenario(
        EntityUid Apc,
        Entity<ApcPowerReceiverComponent> LifeSupport,
        Entity<ApcPowerReceiverComponent> General);
}
