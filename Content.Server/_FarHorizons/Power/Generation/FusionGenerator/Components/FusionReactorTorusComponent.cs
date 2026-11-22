using Content.Shared.Atmos;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent]
public sealed partial class FusionReactorTorusComponent : Component
{
    #region Magnet Variables
    [ViewVariables]
    public bool IsMagnet = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Temperature = Atmospherics.T20C;

    /// <summary>
    /// Thermal mass of the magnet
    /// </summary>
    [DataField]
    public float ThermalMass = 200 * 430; // 200kg of YBaCuO 

    /// <summary>
    /// The superconducting (critical) temperature of the magnet. It will only provide magnetic pressure when colder than this.
    /// </summary>
    [DataField]
    public float TC = 93; // Critical temperature of YBaCuO

    /// <summary>
    /// If the magnet is currently superconducting
    /// </summary>
    [ViewVariables]
    public bool Superconducting => Temperature < TC;

    /// <summary>
    /// Joules stored within the magnet's windings
    /// </summary>
    [ViewVariables]
    public float Charge = 0f;

    /// <summary>
    /// The fraction of charge the magnet uses per second to create pressure
    /// </summary>
    [DataField]
    public float ChargeUse = 0.25f; 

    /// <summary>
    /// Percent of power that gets turned to heat
    /// </summary>
    /// <remarks>Not realistic, but will prevent people from raising pressures to ludicrous levels</remarks>
    [DataField]
    public float Loss = 0.01f;
    #endregion
}