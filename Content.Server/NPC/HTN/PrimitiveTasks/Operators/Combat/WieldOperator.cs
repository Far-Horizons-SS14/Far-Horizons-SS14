using Content.Server.Hands.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

/// <summary>
/// Operator that wields or unwields a weapon.
/// </summary>
public sealed partial class WieldOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    /// <summary>
    /// If true, tries to wield the item. If false, tries to unwield it.
    /// </summary>
    [DataField]
    public bool Wield = true;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return HTNOperatorStatus.Failed;

        var handsSystem = _entManager.System<HandsSystem>();
        if (!handsSystem.TryGetHeldItem(owner, activeHand, out var weaponUid))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<WieldableComponent>(weaponUid, out var wieldable))
            return Wield ? HTNOperatorStatus.Failed : HTNOperatorStatus.Finished; // FH - Failed -> Failed if we're asking "is it wielded", Finished if we're asking "is it unwielded", since if it's not wieldable it's always unwielded.

        var wieldableSystem = _entManager.System<SharedWieldableSystem>();

        if (Wield)
        {
            if (wieldable.Wielded)
                return HTNOperatorStatus.Failed;

            return wieldableSystem.TryWield(weaponUid.Value, wieldable, owner)
                ? HTNOperatorStatus.Finished
                : HTNOperatorStatus.Failed;
        }

        if (!wieldable.Wielded)
            return HTNOperatorStatus.Finished; // FH - Failed -> Finished, so that unweilding is considered successful if the weapon is already unwielded.

        return wieldableSystem.TryUnwield(weaponUid.Value, wieldable, owner)
            ? HTNOperatorStatus.Finished
            : HTNOperatorStatus.Failed;
    }
}
