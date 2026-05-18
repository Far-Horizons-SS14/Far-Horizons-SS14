using System.Diagnostics.CodeAnalysis;
using Content.Server._FarHorizons.Fusion.Systems;
using Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;
using Content.Shared.NodeContainer;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;

public sealed partial class FusionReactorSystem : EntitySystem
{
    [Dependency] private readonly FusionSystem _fusionSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

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
    }

    public void AddReactor(FusionReactorNodeGroup nodeGroup) => _fusionReactors.Add(nodeGroup);

    public void RemoveReactor(FusionReactorNodeGroup nodeGroup) => _fusionReactors.Remove(nodeGroup);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach(var reactor in _fusionReactors)
        {
            // TODO: not every update
            ProcessReactor(reactor);
        }
    }

    private void ProcessReactor(FusionReactorNodeGroup fusionReactor)
    {
        var time = _gameTiming.CurTime;
        _fusionSystem.React(fusionReactor.Plasma, (time - fusionReactor.LastProcess).TotalSeconds);
        fusionReactor.LastProcess = time;

        ProcessCooling(fusionReactor.Magnets, fusionReactor.CoolantIn);

        _atmosphereSystem.Merge(fusionReactor.CoolantOut, fusionReactor.CoolantIn.RemoveVolume(fusionReactor.CoolantOut.Volume));

        // Process magnets and energy draw
        // Process energy export
        
    }
    
    private bool TryGetReactorGroup(EntityUid uid, [NotNullWhen(true)] out FusionReactorNodeGroup? reactorNodeGroup)
    {
        reactorNodeGroup = null;
        if(!EntityManager.TryGetComponent<NodeContainerComponent>(uid, out var nodeContainer))
            return false;
        
        if(!nodeContainer.Nodes.TryGetValue("reactor", out var node))
            return false;
        
        if(node.NodeGroup is not FusionReactorNodeGroup nodeGroup || nodeGroup == null )
            return false;

        reactorNodeGroup = nodeGroup;
        return true;
    }
}