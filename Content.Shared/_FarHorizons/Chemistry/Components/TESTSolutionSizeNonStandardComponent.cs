using Robust.Shared.GameObjects;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// validation exemption for an entity prototype whose solution capacity intentionally differs from its inventory footprint.
/// </summary>
[RegisterComponent]
public sealed partial class TESTSolutionSizeNonStandardComponent : Component
{
    [DataField(required: true)]
    public string Reason = string.Empty;
}
