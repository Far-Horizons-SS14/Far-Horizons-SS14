using Content.Server._FarHorizons.Fusion;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent]
public sealed partial class FusionReactorControllerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float WattSetting = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TempSetting = 1000000;

    [ViewVariables(VVAccess.ReadWrite)]
    public FusionReactorPowerExtractType ExtractMode = FusionReactorPowerExtractType.Temperature;
}

public enum FusionReactorPowerExtractType
{
    Disabled,
    Watts,
    Temperature,
}