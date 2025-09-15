namespace Content.Shared.FarHorizons.Tools.Shipyard.Components;

[RegisterComponent]
public sealed partial class ShipLabelerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int NameMaxChars = 20;

    [DataField]
    public bool NoChecks = false;
}
