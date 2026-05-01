using Content.Server._FarHorizons.Fusion;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator;

[RegisterComponent]
public sealed partial class FusionReactorTorusComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public FusionMixture FusionMixture = new()
    {
        Atoms = {{new(1,0), 10}}
    };

    [ViewVariables(VVAccess.ReadWrite)]
    public double MagneticPressure = 1000;
}