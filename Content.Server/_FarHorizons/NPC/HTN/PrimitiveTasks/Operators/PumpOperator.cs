using Content.Server.Hands.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.HTN;
using Robust.Server.GameObjects;

namespace Content.Server._FarHorizons.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Operator that racks the bolt of a gun with <see cref="ChamberMagazineAmmoProviderComponent"/>.
/// </summary>
public sealed partial class PumpOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        
        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return HTNOperatorStatus.Failed;

        var handsSystem = _entManager.System<HandsSystem>();
        if (!handsSystem.TryGetHeldItem(owner, activeHand, out var gunUid) || gunUid == null)
            return HTNOperatorStatus.Failed;

        if(!_entManager.TryGetComponent<BallisticAmmoProviderComponent>(gunUid, out var ballisticComp))
            return HTNOperatorStatus.Failed;

        var gunSystem = _entManager.System<SharedGunSystem>();
        var xFormSystem = _entManager.System<TransformSystem>();
        gunSystem.ManualCycle((gunUid.Value, ballisticComp), xFormSystem.GetMapCoordinates(owner), owner);

        return HTNOperatorStatus.Finished;
    }
}
