using System.Linq;
using Content.Server._FarHorizons.Fusion.Reactions;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Fusion.Systems;

public sealed partial class FusionSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private FusionReactionPrototype[] _fusionReactions = [];
    private FusionDecayPrototype[] _fusionDecays = [];

    public void CollectReactions()
    {
        _fusionReactions = _protoMan.EnumeratePrototypes<FusionReactionPrototype>().ToArray();
        _fusionDecays = _protoMan.EnumeratePrototypes<FusionDecayPrototype>().ToArray();
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
        // Antimatter reaction gets a special spot to itself
        if (fusionMix.Atoms.TryGetValue(new(-1, 0), out var antiProton) &&
            fusionMix.Atoms.TryGetValue(new(1, 0), out var proton))
        {
            var reactant = Math.Min(antiProton, proton);
            fusionMix.Atoms[new(-1, 0)] -= reactant;
            fusionMix.Atoms[new(1, 0)] -= reactant;

            // E=MC^2
            fusionMix.AddJoule(reactant * FusionConsts.MolToAtom * 2 * FusionConsts.MProton * FusionConsts.C * FusionConsts.C);
        }

        foreach (var reaction in _fusionReactions)
        {
            if (!fusionMix.Atoms.TryGetValue(reaction.ReactantA, out var reactantA) ||
                !fusionMix.Atoms.TryGetValue(reaction.ReactantB, out var reactantB))
                continue;

            DebugTools.Assert(reactantA >= 0 && reactantB >= 0, "Cannot process negative mass");

            reaction.React(fusionMix, deltaT);
        }

        foreach (var decay in _fusionDecays)
        {
            if (!fusionMix.Atoms.TryGetValue(decay.Reactant, out var reactant))
                continue;

            DebugTools.Assert(reactant >= 0, "Cannot process negative mass");

            decay.React(fusionMix, deltaT);
        }
    }

    /// <summary>
    /// Divides a source fusion mixture into several recipient mixtures, scaled by their relative constrained volumes.
    /// </summary>
    /// <remarks>
    /// Serves the same function as AtmosphereSystem.DivideInto
    /// </remarks>
    public void DivideInto(FusionMixture source, List<FusionMixture> receivers)
    {
        // TODO: big maths
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
                    sourceHeatCap ??= source.HeatCap;
                    var receiverHeatCap = receiver.HeatCap;
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

    public void Merge(FusionMixture receiver, FusionMixture source)
    {
        if (Math.Abs(source.Temperature - receiver.Temperature) >= 0.01)
        {
            var sourceHeatCap = source.HeatCap;
            var receiverHeatCap = receiver.HeatCap;
            var combinedHeatCap = receiverHeatCap + sourceHeatCap;
            if (combinedHeatCap > 0.003)
                receiver.Temperature = ((source.Temperature * sourceHeatCap) + (receiver.Temperature * receiverHeatCap)) / combinedHeatCap;
        }

        foreach (var kvp in source.Atoms)
        {
            receiver.Atoms[kvp.Key] = receiver.Atoms.GetValueOrDefault(kvp.Key) + kvp.Value;
        }
    }
}