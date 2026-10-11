using Content.Server.NPC;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server._FarHorizons.NPC.Queries.Considerations;

/// <summary>
/// Returns 1f when target cartridge ammo is unspent, 0f when spent or no cartridge ammo component.
/// </summary>
public sealed partial class TargetAmmoUnspentCon : ExternalConsideration
{
    public override float GetScore(NPCBlackboard blackboard, EntityUid targetUid, IEntityManager entMan) => 
        !entMan.TryGetComponent<CartridgeAmmoComponent>(targetUid, out var cartridgeAmmo) ? 0f : cartridgeAmmo.Spent ? 0f : 1f;
}
