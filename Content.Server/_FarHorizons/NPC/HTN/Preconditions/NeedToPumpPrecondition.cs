using Content.Server.Hands.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Server.NPC.HTN.Preconditions;
using Content.Server.NPC;
using System.Linq;

namespace Content.Server._FarHorizons.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the NPC's gun needs to be cycled.
/// </summary>
public sealed partial class NeedToPumpPrecondition : HTNPrecondition
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

        if (!_entManager.TryGetComponent<GunComponent>(heldEntity, out var gunComp) || !gunComp.Pump)
            return false;

        if (!_entManager.TryGetComponent<BallisticAmmoProviderComponent>(heldEntity, out var ballisticAmmoComp) || !ballisticAmmoComp.Cycleable)
            return false;

        return (ballisticAmmoComp.UnspawnedCount >= 0 || ballisticAmmoComp.Entities.Count > 0) && ballisticAmmoComp.Entities.Count(x => _entManager.TryGetComponent<CartridgeAmmoComponent>(x, out var cartridge) && cartridge.Spent) > 0;
    }
}
