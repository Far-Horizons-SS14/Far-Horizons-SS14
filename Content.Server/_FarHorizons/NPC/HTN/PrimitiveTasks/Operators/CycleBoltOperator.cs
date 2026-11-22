using Content.Server.Hands.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server._FarHorizons.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Opens and closes a manual bolt over successive updates.
/// </summary>
/// <remarks>
/// Very similar to RackBoltOperator, but handles non-rackable bolts that need toggling, and thus also handles
/// cycling back to the same operator in HTN smoothly.
/// </remarks>
public sealed partial class CycleBoltOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return HTNOperatorStatus.Failed;

        var handsSystem = _entManager.System<HandsSystem>();
        if (!handsSystem.TryGetHeldItem(owner, activeHand, out var gunUid))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(gunUid, out var chamberMagazine)
            || chamberMagazine.CanRack
            || chamberMagazine.BoltClosed == null)
            return HTNOperatorStatus.Failed;

        var gunSystem = _entManager.System<SharedGunSystem>();
        gunSystem.ToggleBolt(gunUid.Value, chamberMagazine, owner);

        return chamberMagazine.BoltClosed == false ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Finished;
    }
}
