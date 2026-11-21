using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

/// <summary>
/// Drops the active hand entity underneath us.
/// </summary>
public sealed partial class DropOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue(NPCBlackboard.ActiveHand, out string? activeHand, _entManager))
        {
            return HTNOperatorStatus.Finished;
        }

        var owner = blackboard.GetValueOrDefault<EntityUid>(NPCBlackboard.Owner, _entManager);
        // TODO: Need some sort of interaction cooldown probably.
        var handsSystem = _entManager.System<HandsSystem>();

        // Far Horizons start
        handsSystem.TryDrop(owner); // we actually don't care if we can't drop the item, most likely cause of it would be empty hand, and if there's nothing to drop - that's the same outcome
        return HTNOperatorStatus.Finished;
        // Far Horizons end
    }
}
