using Robust.Shared.Serialization;

namespace Content.Shared.Store.Conditions;

/// <summary>
/// Limits a listing's purchases across all revolutionary uplinks.
/// The server updates <see cref="CurrentStock"/> after each completed purchase.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class StockLimitedListingCondition : ListingCondition
{
    [DataField("stockLimit")]
    public int StockLimit = 1;

    [DataField("currentStock")]
    public int CurrentStock;

    [DataField("lastPurchaser")]
    public string? LastPurchaser;

    public override bool Condition(ListingConditionArgs args)
    {
        return CurrentStock > 0;
    }
}
