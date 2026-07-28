using Content.Shared._FarHorizons.Fusion;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent]
public sealed partial class FusionReactorControllerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float WattSetting = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TempSetting = 5000000;

    [ViewVariables(VVAccess.ReadWrite)]
    public FusionReactorPowerExtractType ExtractMode = FusionReactorPowerExtractType.Temperature;

    [ViewVariables(VVAccess.ReadWrite)]
    public float RequestedMagneticPressure = 1000;

    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<FusionAtom, FusionReactorTransferData> Transfers = [];
}
