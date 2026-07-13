using Content.Server._FarHorizons.Fusion.Systems;
using Content.Shared._FarHorizons.Fusion;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Fusion.Reactions;

[Prototype]
public sealed partial class FusionDecayPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public FusionAtom Reactant;

    /// <summary>
    /// It's a List (FusionAtom, double) at heart
    /// </summary>
    [DataField]
    public Dictionary<Vector2i, double> Products { get; private set; } = [];

    [DataField("energy")]
    public double EnergyPerReaction = 0;

    [DataField("halflife")]
    public double Halflife = 10000;

    public void React(FusionMixture mixture, double deltaT, FusionSystem fusionSystem)
    {
        var reactant = mixture.Atoms.GetValueOrDefault(Reactant);
        var processAmount = reactant - (reactant * Math.Pow(0.5, deltaT / Halflife));
        var count = Math.Round(processAmount * FusionConsts.MolToAtom);

        if (count <= 0 || double.IsNaN(processAmount))
            return;

        mixture.ChangeAtom(Reactant, -processAmount);

        foreach (var (atom, amount) in Products)
        {
            mixture.ChangeAtom(atom, processAmount * amount);
        }

        if (EnergyPerReaction == 0)
            return;

        var joules = EnergyPerReaction * count * FusionConsts.EVToJoule* fusionSystem.EnergyScale;

        fusionSystem.AddJoule(mixture, joules);
        mixture._debug_EnergySources[ID] = joules;
    }
}