using Content.Shared.Atmos;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent]
public sealed partial class FusionReactorTorusComponent : Component
{
    #region Magnet Variables
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsMagnet = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Temperature = Atmospherics.T20C;

    [DataField]
    public float ThermalMass = 200 * 430; // 200kg of YBaCuO 

    [DataField]
    public float TC = 93; // Critical temperature of YBaCuO

    [ViewVariables]
    public bool Superconducting => Temperature < TC;

    /// <summary>
    /// Percent of power that gets turned to heat
    /// </summary>
    /// <remarks>Not realistic, but will prevent people from raising pressures to ludicrous levels</remarks>
    [DataField]
    public float Loss = 0.01f;
    #endregion
}