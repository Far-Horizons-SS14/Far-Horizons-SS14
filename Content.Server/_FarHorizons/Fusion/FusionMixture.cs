using System.Linq;
using Content.Shared.Atmos;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Fusion;

public sealed class FusionMixture
{
    /// <summary>
    /// Key: atom<para/>Value: mols
    /// </summary>
    [DataField("atoms")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<FusionAtom, double> Atoms = [];

    /// Unlike in a <see cref="GasMixture"/>, the pressure is the independent variable and the volume is the dependent
    /// This is due to <see cref="FusionMixture"/> being contained mostly by magnetic pressure, rather than the volume of the container
    /// Should the <see cref="Volume"/> expand beyond the capacity of the container, the container should start recieving damage

    /// <summary>
    /// Pressure in Pa
    /// </summary>
    [DataField("pressure")]
    [ViewVariables(VVAccess.ReadWrite)]
    public double Pressure = 1;

    /// <summary>
    /// Volume, in liters
    /// </summary>
    [ViewVariables]
    public double Volume => TotalMoles * FusionConsts.R * Temperature / Pressure;

    /// <summary>
    /// Volume of the physical container the <see cref="FusionMixture"/> is within
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public double ConstrainedVolume = Atmospherics.CellVolume;

    /// <summary>
    /// Pressure of the <see cref="FusionMixture"/> against its physical container, used to determine how much damage is being done 
    /// </summary>
    [ViewVariables]
    public double ConstrainedPressure => (Volume < ConstrainedVolume) ? 0 : (TotalMoles * FusionConsts.R * Temperature / ConstrainedVolume) - Pressure;

    /// <summary>
    /// Temperature of the mixture, in kelvin
    /// </summary>
    [DataField("temperature")]
    [ViewVariables(VVAccess.ReadWrite)]
    public double Temperature
    {
        get;
        set
        {
            DebugTools.Assert(!double.IsNaN(value));
            field = Math.Min(Math.Max(value, FusionConsts.PlasmaTemperature), FusionConsts.PlanckTemperature);
        }
    } = FusionConsts.PlasmaTemperature;

    /// <summary>
    /// Joules of energy that, due to floating point limitations, cannot be expressed as temperature  
    /// </summary>
    [ViewVariables]
    private double _joules;

    [ViewVariables]
    public double HeatCap => TotalMoles * FusionConsts.HeatCap;

    [ViewVariables]
    public double TotalMoles => Atoms.Values.Sum();

    public void AddEV(double electronVolts) => AddJoule(electronVolts * FusionConsts.EVToJoule);

    public void AddJoule(double joules)
    {
        _joules += joules;
        _debug_MadeJoules += joules;

        // work around for floating point imprecision
        var prevTemp = Temperature;
        Temperature += _joules / HeatCap;
        _joules -= (Temperature - prevTemp) * HeatCap;
    }

    public void ChangeAtom(FusionAtom key, double change) => Atoms[key] = Atoms.GetValueOrDefault(key) + change;

    #region Debug Vars
    [ViewVariables]
    private double _debug_MadeJoules;
    [ViewVariables]
    public Dictionary<string, double> _debug_EnergySources = [];
    #endregion
}

[DataDefinition]
public partial record struct FusionAtom
{
    [DataField]
    public int Proton { get; private set; }
    [DataField]
    public int Neutron { get; private set; }

    public FusionAtom(int proton, int neutron)
    {
        Proton = proton;
        Neutron = neutron;
    }

    // Makes prototype loading easier
    public static implicit operator FusionAtom(Vector2i vector) => new(vector.X, vector.Y);
}