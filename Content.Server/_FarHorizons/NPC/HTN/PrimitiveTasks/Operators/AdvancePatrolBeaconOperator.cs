using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared._FarHorizons.NPC;
using Content.Shared.Pinpointer;

namespace Content.Server._FarHorizons.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class AdvancePatrolBeaconOperator : HTNOperator
{
    [Dependency] private IEntityManager _entityManager = default!;

    [DataField]
    public string TargetBeaconKey = "PatrolBeaconTarget";

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entityManager.TryGetComponent(owner, out SimpleBotConfigurationComponent? configuration) ||
            configuration.PatrolMode != SimpleBotPatrolMode.Patrol ||
            !blackboard.TryGetValue<EntityUid>(TargetBeaconKey, out var beacon, _entityManager) ||
            !_entityManager.TryGetComponent(beacon, out NavMapBeaconComponent? route) ||
            string.IsNullOrWhiteSpace(route.NextBeacon))
        {
            return HTNOperatorStatus.Finished;
        }

        _entityManager.System<SimpleBotConfigurationSystem>()
            .SetPatrol((owner, configuration), configuration.PatrolMode, route.NextBeacon);
        return HTNOperatorStatus.Finished;
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        blackboard.Remove<EntityUid>(TargetBeaconKey);
    }
}
