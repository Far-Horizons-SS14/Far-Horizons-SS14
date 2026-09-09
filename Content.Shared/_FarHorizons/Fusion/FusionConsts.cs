using Content.Shared.Atmos;

namespace Content.Shared._FarHorizons.Fusion;

public static class FusionConsts
{
    /// <summary>
    /// Speed of light in m/s
    /// </summary>
    public const int C = 299792458;

    /// <summary>
    /// Fine structure constant
    /// </summary>
    public const double A = 0.0072973525643;

    /// <summary>
    /// Boltzmann constant
    /// </summary>
    public const double KB = 1.380649e-23;

    /// <summary>
    /// Mass of a single proton in kg
    /// </summary>
    public const double MProton = 1.67262192e-27;

    /// <summary>
    /// Mass of a single neutron in kg
    /// </summary>
    public const double MNeutron = 1.67492750e-27;

    /// <summary>
    /// The universal gas constant, in kPa*L/(K*mol)
    /// </summary>
    public const double R = Atmospherics.R;

    /// <summary>
    /// Approximation for the heat capacity of a mol of monoatomic gas
    /// </summary>
    /// <remarks>Monoatomic gases are weird like that</remarks>
    public const double HeatCap = 1.5 * R;

    /// <summary>
    /// Minimum number, based on the smallest number that will effect 1   
    /// </summary>
    public const double MinQuantity = 1e-16;

    /// <summary>
    /// Multiplier to convert from Electronvolts to Joules
    /// </summary>
    public const double EVToJoule = 1.60218e-19;

    /// <summary>
    /// Multiplier to convert from Joules to Electronvolts
    /// </summary>
    public const double JouleToEV = 6.242e+18;

    /// <summary>
    /// Multiplier to convert from mols to atoms
    /// </summary>
    /// <remarks>Avogadro constant</remarks>
    public const double MolToAtom = 6.02214076e+23;

    /// <summary>
    /// Multiplier to convert from atoms to mols
    /// </summary>
    public const double AtomToMol = 1.66053907e-24;

    /// <summary>
    /// Minimum temperature of a plamsa, roughly the temperature of an iron plasma 
    /// </summary>
    public const float PlasmaTemperature = 7000;

    /// <summary>
    /// Absolute hot
    /// </summary>
    public const double PlanckTemperature = 1.416784e32;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="atom"></param>
    /// <returns>Mass of input atom in kg</returns>
    public static double AtomMass(FusionAtom atom) => (MProton * atom.Proton) + (MNeutron * atom.Neutron);

    public static double AtomRadius(FusionAtom atom) => 1.2e-15 * Math.Cbrt(atom.Proton + atom.Neutron);
    public static double AtomRadius(FusionAtom atom1, FusionAtom atom2) => 1.2e-15 * Math.Cbrt((atom1.Proton + atom1.Neutron + atom2.Proton + atom2.Neutron) * 0.5);
}