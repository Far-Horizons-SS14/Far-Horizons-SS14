using Content.Shared._FarHorizons.Fusion;
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

    [ViewVariables(VVAccess.ReadWrite)]
    public FusionMixture Stored = new();

    [ViewVariables]
    public GasMixture CoolantIn = new();
    [ViewVariables]
    public GasMixture CoolantOut = new();

    [ViewVariables]
    public double MagneticPressure { get; set; } = 1000;
    [ViewVariables(VVAccess.ReadWrite)]
    public float RequestedMagneticPressure { get; set; } = 1000;
}