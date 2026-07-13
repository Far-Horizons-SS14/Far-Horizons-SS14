namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent]
public sealed partial class FusionReactorPowerSupplyComponent : Component
{
    /// <summary>
    /// If it is supplying power
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = false;

    /// <summary>
    /// Power supply, in watts
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Supply = 0;

    /// <summary>
    /// How much power did not go into storage
    /// </summary>
    [ViewVariables]
    public float Surplus = 0;
}