using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[AutoGenerateComponentPause]
public sealed partial class MagnetPickupComponent : Component
{
    [DataField]
    [AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan NextScan = TimeSpan.Zero;

    /// <summary>
    /// What container slot the magnet needs to be in to work.
    /// </summary>
    [DataField]
    public SlotFlags? SlotFlags = Inventory.SlotFlags.BELT;

    // Starlight Edit Start - Remove slot requirements
    /// <summary>
    /// Controlling whether the magnet needs to be equipped to work,
    /// if false, it will work on the ground and in hands,
    /// if true it wont work at all if not equipped.
    /// This also makes it work in borg modules.
    /// </summary>
    [DataField]
    public bool RequiresEquipping = true;
    // Starlight Edit End

    [DataField]
    public bool RequireActiveHand = false;

    [DataField]
    public float Range = 1f;
}
