using System.Linq;
using Content.Server._FarHorizons.Fusion.Reactions;
using Content.Shared._FarHorizons.Fusion;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Fusion.Systems;

public sealed partial class FusionSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private FusionReactionPrototype[] _fusionReactions = [];
    private FusionDecayPrototype[] _fusionDecays = [];
    private FusionConversionPrototype[] _fusionConversions = [];

    public void CollectReactions()
    {
        _fusionReactions = _protoMan.EnumeratePrototypes<FusionReactionPrototype>().ToArray();
        _fusionDecays = _protoMan.EnumeratePrototypes<FusionDecayPrototype>().ToArray();
        _fusionConversions = _protoMan.EnumeratePrototypes<FusionConversionPrototype>().ToArray();
    }

    public static double FusionAmount(FusionAtom atom1, FusionAtom atom2, double temperature)
    {
        var m1 = FusionConsts.AtomMass(atom1);
        var m2 = FusionConsts.AtomMass(atom2);
        var kBT = FusionConsts.KB * temperature;

        /// I can't even properly explain this, just know it works
        /// https://en.wikipedia.org/wiki/Stellar_nucleosynthesis#Reaction_rate
        /// Even if it "works" it's a terrible approximation and should probably be re-done by someone who knows what they're doing 
        var u = m1 * m2 / (m1 + m2);
        var EG = 2 * u * Sqr(FusionConsts.C) * Sqr(Math.PI * FusionConsts.A * atom1.Proton * atom2.Proton);
        var E0 = Math.Cbrt(EG * Sqr(kBT / 2));

        return 4 * Math.Sqrt(2 * u / 3) / u * Math.Sqrt(E0) * (S(E0) / kBT) * Math.Exp(-3 * E0 / kBT);

        double Sqr(double n) => n * n;
        double S(double E) => E / Math.Exp(-Math.Sqrt(EG / E)) * Math.PI * Sqr(FusionConsts.AtomRadius(atom1, atom2));
    }

    public void React(FusionMixture fusionMix, double deltaT)
    {
        // During reactor construction it may pass a NaN fusion mixture
        if (double.IsNaN(fusionMix.TotalMoles))
            return;

        /// Antimatter reaction gets a special spot to itself
        /// It is assumed that neutrons cease to exist when the matter/antimatter reacts
        var antimatterAtoms = fusionMix.Atoms.Where(k => k.Key.Proton < 0 && k.Value > 0).ToList();
        if (antimatterAtoms.Count > 0)
        {
            /// Calculate how much antimatter reacts
            var antimatter = antimatterAtoms.Sum(k => k.Value * -k.Key.Proton);
            var matter = fusionMix.Atoms.Where(k => k.Key.Proton > 0).Sum(k => k.Value * k.Key.Proton);
            var reactQty = Math.Min(antimatter, matter);
            var reactAnti = reactQty;
            var reactMatter = reactQty;

            /// Remove the reacted mols from the mixture
            foreach (var (atom, mol) in fusionMix.Atoms)
            {
                if (atom.Proton < 0 && reactAnti > 0)
                {
                    var amount = Math.Min(reactAnti, mol * -atom.Proton);
                    fusionMix.ChangeAtom(atom, amount / atom.Proton);
                    reactAnti -= amount;
                }
                else if (atom.Proton > 0 && reactMatter > 0)
                {
                    var amount = Math.Min(reactMatter, mol * atom.Proton);
                    fusionMix.ChangeAtom(atom, -amount / atom.Proton);
                    reactMatter -= amount;
                }

                // Mass dues are paid, no need to waste our time searching further
                if (reactAnti <= 0 && reactMatter <= 0)
                    break;
            }

            /// E=MC^2
            ChangeJoule(fusionMix, reactQty * FusionConsts.MolToAtom * 2 * FusionConsts.MProton * FusionConsts.C * FusionConsts.C);
        }

        foreach (var reaction in _fusionReactions)
        {
            if (!fusionMix.Atoms.TryGetValue(reaction.ReactantA, out var reactantA) ||
                !fusionMix.Atoms.TryGetValue(reaction.ReactantB, out var reactantB))
                continue;

            DebugTools.Assert(reactantA >= 0 && reactantB >= 0, "Cannot process negative mass");

            reaction.React(fusionMix, deltaT, this);
        }

        foreach (var decay in _fusionDecays)
        {
            if (!fusionMix.Atoms.TryGetValue(decay.Reactant, out var reactant))
                continue;

            DebugTools.Assert(reactant >= 0, "Cannot process negative mass");

            decay.React(fusionMix, deltaT, this);
        }
    }

    /// <summary>
    /// Gets the heat capacity of <paramref name="mixture"/>.
    /// </summary>
    /// <param name="mixture"><see cref="FusionMixture"/> to get the heat capacity of.</param>
    /// <param name="scale">If it should be scaled by CVars.</param>
    /// <returns>Heat capacity of <paramref name="mixture"/>.</returns>
    public double GetHeatCapacity(FusionMixture mixture, bool scale = true) =>
        mixture.TotalMoles * FusionConsts.HeatCap * (scale ? HeatScale : 1);

    /// <summary>
    /// Gets the thermal energy of <paramref name="mixture"/>.
    /// </summary>
    /// <param name="mixture"><see cref="FusionMixture"/> to get the thermal energy of.</param>
    /// <param name="scale">If it should be scaled by CVars.</param>
    /// <returns>Thermal energy of <paramref name="mixture"/>.</returns>
    public double GetThermalEnergy(FusionMixture mixture, bool scale = true) =>
        mixture.Temperature * GetHeatCapacity(mixture, scale);

    /// <summary>
    /// Changes the energy in <paramref name="mixture"/> by a number of electron volts.
    /// </summary>
    /// <param name="mixture">Mixture to have its energy changed.</param>
    /// <param name="electronVolts">Amount of energy, in electron volts.</param>
    public void ChangeEV(FusionMixture mixture, double electronVolts) =>
        ChangeJoule(mixture, electronVolts * FusionConsts.EVToJoule);

    /// <summary>
    /// Changes the energy in <paramref name="mixture"/> by a number of joules.
    /// </summary>
    /// <param name="mixture">Mixture to have its energy changed.</param>
    /// <param name="joules">Amount of energy, in joules.</param>
    /// <returns>Actual change</returns>
    public double ChangeJoule(FusionMixture mixture, double joules)
    {
        // Can't change the energy state of a vacuum.
        if (mixture.TotalMoles <= 0)
            return 0;

        var heatCap = GetHeatCapacity(mixture);
        var maxRemovableJoules = heatCap * (mixture.Temperature - FusionConsts.PlasmaTemperature);
        var jouleChange = joules <= -maxRemovableJoules ? -maxRemovableJoules : joules;
        mixture.Joules += jouleChange;

        // work around for floating point imprecision
        var prevTemp = mixture.Temperature;
        mixture.Temperature += jouleChange / heatCap;
        mixture.Joules -= (mixture.Temperature - prevTemp) * heatCap;

        return jouleChange;
    }

    /// <summary>
    /// Divides a source fusion mixture into several recipient mixtures, scaled by their relative 
    /// constrained volumes.
    /// </summary>
    /// <remarks>
    /// Serves the same function as AtmosphereSystem.DivideInto
    /// </remarks>
    public void DivideInto(FusionMixture source, List<FusionMixture> receivers)
    {
        var totalVolume = 0d;
        foreach (var receiver in receivers)
        {
            totalVolume += receiver.ConstrainedVolume;
        }

        double? sourceHeatCap = null;

        foreach (var receiver in receivers)
        {
            var fraction = receiver.ConstrainedVolume / totalVolume;

            if (Math.Abs(source.Temperature - receiver.Temperature) >= 0.01)
            {
                if (receiver.TotalMoles == 0)
                    receiver.Temperature = source.Temperature;
                else
                {
                    sourceHeatCap ??= GetHeatCapacity(source);
                    var receiverHeatCap = GetHeatCapacity(receiver);
                    var combinedHeatCap = receiverHeatCap + (sourceHeatCap.Value * fraction);
                    if (combinedHeatCap > 0.003)
                        receiver.Temperature = ((source.Temperature * sourceHeatCap.Value * fraction) + (receiver.Temperature * receiverHeatCap)) / combinedHeatCap;
                }
            }

            receiver.Pressure = source.Pressure;
            foreach (var kvp in source.Atoms)
            {
                receiver.Atoms[kvp.Key] = receiver.Atoms.GetValueOrDefault(kvp.Key) + (kvp.Value * fraction);
            }
        }
    }

    /// <summary>
    /// Merges the source mixture into the receiver mixture. The source mixture is unmodified.
    /// </summary>
    public void Merge(FusionMixture receiver, FusionMixture source)
    {
        if (Math.Abs(source.Temperature - receiver.Temperature) >= 0.01)
        {
            var sourceHeatCap = GetHeatCapacity(source);
            var receiverHeatCap = GetHeatCapacity(receiver);
            var combinedHeatCap = receiverHeatCap + sourceHeatCap;
            if (combinedHeatCap > 0.003)
                receiver.Temperature = ((source.Temperature * sourceHeatCap) + (receiver.Temperature * receiverHeatCap)) / combinedHeatCap;
        }

        foreach (var kvp in source.Atoms)
        {
            receiver.Atoms[kvp.Key] = receiver.Atoms.GetValueOrDefault(kvp.Key) + kvp.Value;
        }
    }

    /// <summary>
    /// Converts a gas mixture into a fusion mixture according the the conversion prototypes.
    /// </summary>
    /// <param name="input">Input gas mixture</param>
    /// <returns>Resultant fusion mixture</returns>
    public FusionMixture ConvertFromGasMixture(GasMixture input)
    {
        FusionMixture mixture = new();

        foreach (var conversion in _fusionConversions)
        {
            // Sanity check that should stop array errors
            if ((int)conversion.Gas >= Atmospherics.TotalNumberOfGases)
                continue;

            var gas = input.GetMoles(conversion.Gas);
            if (gas <= 0)
                continue;

            foreach (var (atom, amount) in conversion.Products)
            {
                mixture.ChangeAtom(atom, amount * gas);
            }
        }

        return mixture;
    }

    public double GetMass(FusionMixture mixture)
    {
        double mass = 0;
        foreach (var (atom, mol) in mixture.Atoms)
        {
            mass += ((atom.Neutron * FusionConsts.MNeutron) + (Math.Abs(atom.Proton) * FusionConsts.MProton))
                * FusionConsts.MolToAtom * mol;
        }
        return mass;
    }
}