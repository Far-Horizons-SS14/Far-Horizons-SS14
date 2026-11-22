using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Movement.Pulling.Systems;

namespace Content.Server._FarHorizons.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Starts pulling the entity stored in a blackboard key.
/// </summary>
public sealed partial class StartPullOperator : HTNOperator
{
    [Dependency] private IEntityManager _entityManager = default!;
    private PullingSystem _pulling = default!;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public bool RemoveTargetOnShutdown = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pulling = sysManager.GetEntitySystem<PullingSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entityManager) ||
            _entityManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        return _pulling.TryStartPull(owner, target)
            ? HTNOperatorStatus.Finished
            : HTNOperatorStatus.Failed;
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        if (RemoveTargetOnShutdown)
            blackboard.Remove<EntityUid>(TargetKey);
    }
}
