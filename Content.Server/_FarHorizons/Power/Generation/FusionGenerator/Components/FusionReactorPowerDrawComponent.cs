namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent]
public sealed partial class FusionReactorPowerDrawComponent : Component
{
    /// <summary>
    /// If it is drawing power
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = false;

    /// <summary>
    /// Power draw, in watts
    /// </summary>
    [DataField]
    public float Draw = 1000;

    /// <summary>
    /// 0-1 of how much of the requested power draw was supplied
    /// </summary>
    [ViewVariables]
    public float Satisfaction = 0;

    /// <summary>
    /// Number of watts actually supplied
    /// </summary>
    [ViewVariables]
    public float Supplied => Enabled ? Satisfaction * Draw : 0;

    /// <summary>
    /// Determines who gets power first. Lower number is higher priority.
    /// </summary>
    [DataField]
    public int Priority = 5;

    /// <summary>
    /// Action invoked after satisfaction is calculated
    /// </summary>
    public Action OnSatisfy;
}