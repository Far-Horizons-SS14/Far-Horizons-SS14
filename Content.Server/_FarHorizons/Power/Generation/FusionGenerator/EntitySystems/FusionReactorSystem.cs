using System.Diagnostics.CodeAnalysis;
using Content.Server._FarHorizons.Fusion.Systems;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.NodeContainer;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly FusionSystem _fusionSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ItemSlotsSystem _slotsSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    /// <summary>
    /// May eventually be handled by a grid/map level component like the atmosphere system, but for now the system can keep track of it.
    /// </summary>
    private readonly List<FusionReactorNodeGroup> _fusionReactors = [];

    public override void Initialize()
    {
        base.Initialize();

        BatteryInitialize();
        ControllerInitialize();
        TorusInitialize();
        CoolingInitialize();
        MaserInitialize();
        GasInletInitialize();
    }

    public void AddReactor(FusionReactorNodeGroup nodeGroup) => _fusionReactors.Add(nodeGroup);

    public void RemoveReactor(FusionReactorNodeGroup nodeGroup) => _fusionReactors.Remove(nodeGroup);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var batteryQuery = EntityQueryEnumerator<FusionReactorBatteryComponent>();

        while (batteryQuery.MoveNext(out var uid, out var battery))
        {
            // Battery UIs update every tick, just like a normal battery
            UpdateBatteryUi(uid, battery);
        }

        foreach (var reactor in _fusionReactors)
        {
            // TODO: not every update
            ProcessReactor(reactor);
        }
    }

    private void ProcessReactor(FusionReactorNodeGroup fusionReactor)
    {
        var time = _gameTiming.CurTime;
        var dt = (float)(time - fusionReactor.LastProcess).TotalSeconds;
        fusionReactor.LastProcess = time;

        ProcessCooling(fusionReactor, dt);
        ProcessMagnetics(fusionReactor, dt);

        ProcessMaser(fusionReactor, dt);

        ProcessInjects(fusionReactor, dt);
        ExtractPower(fusionReactor, dt);

        ProcessPowerDraws(fusionReactor, dt);

        _fusionSystem.React(fusionReactor.Plasma, dt);

        ProcessDamage(fusionReactor.Torus, fusionReactor.Plasma, dt);
    }

    private bool TryGetReactorGroup(EntityUid uid, [NotNullWhen(true)] out FusionReactorNodeGroup? reactorNodeGroup)
    {
        reactorNodeGroup = null;
        if (!EntityManager.TryGetComponent<NodeContainerComponent>(uid, out var nodeContainer))
            return false;

        if (!nodeContainer.Nodes.TryGetValue("reactor", out var node))
            return false;

        if (node.NodeGroup is not FusionReactorNodeGroup nodeGroup || nodeGroup == null)
            return false;

        reactorNodeGroup = nodeGroup;
        return true;
    }
}