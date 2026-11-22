using Content.Server.Hands.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server._FarHorizons.NPC.HTN.Preconditions;

/// <summary>
/// Checks whether a gun without instant racking needs its bolt opened or closed.
/// </summary>
/// <remarks>
/// Basically the same as NeedToRackBoltPrecondition, but for cases where you have a non-rackable bolt.
/// </remarks>
public sealed partial class NeedToCycleBoltPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return false;

        var handsSystem = _entManager.System<HandsSystem>();
        if (!handsSystem.TryGetHeldItem(owner, activeHand, out var heldEntity))
            return false;

        if (!_entManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(heldEntity, out var chamberMagazine))
            return false;

        // If it can rack, we should be racking it instead.
        if (chamberMagazine.CanRack || chamberMagazine.BoltClosed == null)
            return false;

        var gunSystem = _entManager.System<SharedGunSystem>();
        var chamberEntity = gunSystem.GetChamberEntity(heldEntity.Value);
        bool hasRoundInChamber = chamberEntity is not null;

        hasRoundInChamber = hasRoundInChamber && (!_entManager.TryGetComponent<CartridgeAmmoComponent>(chamberEntity!.Value, out var cartridge) || !cartridge.Spent);

        return chamberMagazine.BoltClosed == false || !hasRoundInChamber;
    }
}
