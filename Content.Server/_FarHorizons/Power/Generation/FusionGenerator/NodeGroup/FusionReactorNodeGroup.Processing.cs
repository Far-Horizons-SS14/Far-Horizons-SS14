using Content.Server._FarHorizons.Fusion;
using Content.Shared.Atmos;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.NodeGroup;

public sealed partial class FusionReactorNodeGroup
{
    [ViewVariables(VVAccess.ReadWrite)]
    public FusionMixture Plasma = new()
    {
        Pressure = 1000,
        Atoms = { { new(1, 0), 10 } }
    };

    //TODO: replace this with lists of input and output gas mixtures, which the fusionreactorsystem would then take care of transfering gas and cooling parts

    [ViewVariables]
    public GasMixture CoolantIn = new();
    [ViewVariables]
    public GasMixture CoolantOut = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public double MagneticPressure { get; private set; } = 1000;
}