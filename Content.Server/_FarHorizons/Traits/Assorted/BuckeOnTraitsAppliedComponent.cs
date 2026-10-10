using Robust.Shared.Prototypes;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// Upon applying traits buckles the attached entity to a newly spawned prototype.
/// </summary>
[RegisterComponent, Access(typeof(BuckeOnTraitsAppliedSystem))]
public sealed partial class BuckeOnTraitsAppliedComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public EntProtoId Prototype;
}
