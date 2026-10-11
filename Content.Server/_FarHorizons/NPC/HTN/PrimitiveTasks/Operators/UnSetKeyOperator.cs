using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server._FarHorizons.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class UnSetKeyOperator : HTNOperator
{
    [DataField] public string Key = string.Empty;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.ContainsKey(Key)) 
            return HTNOperatorStatus.Finished;

        blackboard.Remove<EntityUid>(Key);
        return HTNOperatorStatus.Finished;
    }
}