namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent]
public sealed partial class FusionReactorControllerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float PowerExtraction = 0;
}