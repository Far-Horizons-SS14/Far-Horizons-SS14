using Robust.Shared.Serialization;

namespace Content.Shared.Store.Conditions;

/// <summary>
/// Controls a revolutionary listing's availability based on supply-rift state.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RevRiftListingCondition : ListingCondition
{
    /// <summary>
    /// Whether this listing requires a finished supply rift to be active.
    /// </summary>
    [DataField]
    public bool RequiresActiveRift = true;

    /// <summary>
    /// Whether destroying a supply rift permanently disables this listing for the round.
    /// </summary>
    [DataField]
    public bool DisableAfterRiftDestroyed;

    [DataField]
    public bool HasActiveRift;

    [DataField]
    public bool RiftDestroyed;

    public override bool Condition(ListingConditionArgs args)
    {
        if (DisableAfterRiftDestroyed && RiftDestroyed)
            return false;

        return RequiresActiveRift == HasActiveRift;
    }
}
