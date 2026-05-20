using System.Diagnostics.CodeAnalysis;
using Content.Server._FarHorizons.Fusion.Systems;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Server.Power.EntitySystems;
using Content.Shared.NodeContainer;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem : EntitySystem
{
    [Dependency] private readonly FusionSystem _fusionSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    /// <summary>
    /// May eventually be handled by a grid/map level component like the atmosphere system, but for now the system can keep track of it.
    /// </summary>
    private readonly List<FusionReactorNodeGroup> _fusionReactors = [];

    public override void Initialize()
    {
        base.Initialize();

        ControllerInitialize();
        TorusInitialize();
        CoolingInitialize();
        MaserInitialize();
    }

    public void AddReactor(FusionReactorNodeGroup nodeGroup) => _fusionReactors.Add(nodeGroup);

    public void RemoveReactor(FusionReactorNodeGroup nodeGroup) => _fusionReactors.Remove(nodeGroup);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

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

        ProcessMaser(fusionReactor, dt);

        ProcessCooling(fusionReactor, dt);
        ProcessMagnetics(fusionReactor, dt);

        _fusionSystem.React(fusionReactor.Plasma, dt);

        ProcessDamage(fusionReactor.Torus, fusionReactor.Plasma);

        ExtractPower(fusionReactor, dt);
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

    /// <summary>
    /// Takes energy from the <paramref name="reactorNodeGroup"/> to power a function
    /// </summary>
    /// <param name="reactorNodeGroup"></param>
    /// <param name="amount"></param>
    /// <returns>The number of joules taken from the capacitors.</returns>
    private float DrainPower(FusionReactorNodeGroup reactorNodeGroup, float amount)
    {
        if (reactorNodeGroup.Batteries.Count <= 0)
            return 0;

        var dE = amount / reactorNodeGroup.Batteries.Count;
        var output = 0f;
        var deficit = 0f;

        foreach (var (uid, comp) in reactorNodeGroup.Batteries)
        {
            var chargeChange = -_battery.UseCharge(uid, dE);
            chargeChange += -_battery.UseCharge(uid, deficit);
            deficit += dE - chargeChange;
            output += chargeChange;
        }

        return output;
    }

    /// <summary>
    /// Adds energy to the <paramref name="reactorNodeGroup"/>
    /// </summary>
    /// <param name="reactorNodeGroup"></param>
    /// <param name="amount"></param>
    /// <returns>The number of joules added to the capacitors.</returns>
    private float AddPower(FusionReactorNodeGroup reactorNodeGroup, float amount)
    {
        if (reactorNodeGroup.Batteries.Count <= 0)
            return 0;

        var dE = amount / reactorNodeGroup.Batteries.Count;
        var output = 0f;
        var excess = 0f;

        foreach (var (uid, comp) in reactorNodeGroup.Batteries)
        {
            var chargeChange = _battery.ChangeCharge(uid, dE);
            chargeChange += _battery.ChangeCharge(uid, excess);
            excess += dE - chargeChange;
            output += chargeChange;
        }

        return output;
    }
}