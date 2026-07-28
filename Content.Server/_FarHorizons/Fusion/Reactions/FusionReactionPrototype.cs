using Content.Server._FarHorizons.Fusion.Systems;
using Content.Shared._FarHorizons.Fusion;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Fusion.Reactions;

[Prototype]
public sealed partial class FusionReactionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public FusionAtom ReactantA;

    [DataField]
    public FusionAtom ReactantB;

    /// <summary>
    /// It's a List (FusionAtom, double) at heart
    /// </summary>
    [DataField]
    public Dictionary<Vector2i, double> Products { get; private set; } = [];

    [DataField("energy")]
    public double EnergyPerReaction = 0;

    public void React(FusionMixture mixture, double deltaT, FusionSystem fusionSystem)
    {
        var reactantA = mixture.Atoms[ReactantA];
        var reactantB = mixture.Atoms[ReactantB];
        var cm3 = mixture.Volume * 1000;
        var numA = reactantA / cm3 * FusionConsts.MolToAtom;
        var numB = reactantB / cm3 * FusionConsts.MolToAtom;

        var selfReact = ReactantA == ReactantB ? 0.5 : 1;
        /// reactions per cm^3
        var reactionCount = numA * numB * FusionSystem.FusionAmount(ReactantA, ReactantB, mixture.Temperature)
                            * selfReact * deltaT;

        /// convert back to across full mixture
        reactionCount *= cm3;

        var reactionMols = Math.Min(reactionCount * FusionConsts.AtomToMol * fusionSystem.MassScale, Math.Min(reactantA, reactantB) * selfReact);
        // var realmin = FusionConsts.MinQuantity * (reactantA + reactantB);

        if (double.IsNaN(reactionCount) || reactionMols < FusionConsts.MinQuantity || reactionCount < 1)
            return;

        mixture.ChangeAtom(ReactantA, -reactionMols);
        mixture.ChangeAtom(ReactantB, -reactionMols);

        DebugTools.Assert(mixture.Atoms[ReactantA] >= 0 && mixture.Atoms[ReactantB] >= 0, "Consumed more mass than exists");

        foreach (var (atom, amount) in Products)
        {
            mixture.ChangeAtom(atom, reactionMols * amount);
        }

        if (EnergyPerReaction == 0)
            return;

        var joules = EnergyPerReaction * reactionCount * FusionConsts.EVToJoule * fusionSystem.EnergyScale;

        fusionSystem.ChangeJoule(mixture, joules);
        mixture._debug_EnergySources[ID] = joules;
    }
}