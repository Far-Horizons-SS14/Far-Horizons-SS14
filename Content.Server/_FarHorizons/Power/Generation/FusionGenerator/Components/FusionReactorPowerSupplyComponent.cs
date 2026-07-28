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
    /// How much excess power was produced
    /// </summary>
    [ViewVariables]
    public float Surplus = 0;

    /// <summary>
    /// Action invoked after surplus is calculated
    /// </summary>
    public Action OnSurplus;
}