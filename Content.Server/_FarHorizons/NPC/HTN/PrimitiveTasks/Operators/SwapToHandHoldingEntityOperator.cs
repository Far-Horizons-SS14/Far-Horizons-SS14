using Content.Server.Hands.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server._FarHorizons.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SwapToHandHoldingEntityOperator : HTNOperator
{
    [Dependency] private IEntityManager _entMan = default!;
    private HandsSystem _hands = default!;

    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _hands = sysManager.GetEntitySystem<HandsSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var uid, _entMan) ||
            !_hands.TrySelect(owner, uid))
            return HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;
    }
}