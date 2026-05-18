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
    #endregion

    #region Torus Variables
    /// <summary>
    /// The "health" of the torus
    /// </summary>
    [DataField]
    public float Integrity = 1000;

    /// <summary>
    /// The maximum value of <see cref="Integrity"/>
    /// </summary>
    [DataField]
    public float MaxIntegrity = 1000;

    /// <summary>
    /// How resistant is the torus to the pressure of a confinement failure
    /// </summary>
    [DataField]
    public float PressureResistance = 1000;

    /// <summary>
    /// How resistant is the torus to the temperature of a confinement failure
    /// </summary>
    [DataField]
    public float TemperatureResistance = 1300;
    #endregion
}