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

    /// <summary>
    /// Exponent applied to the power output
    /// </summary>
    [DataField]
    public float PowerExponent = 0.95f;

    /// <summary>
    /// The fraction of energy extracted that turns into usable power
    /// </summary>
    [DataField]
    public float PowerEfficiency = 0.35f;

    /// <summary>
    /// Largest portion of energy the controller can extract from the reactor's plasma per second
    /// </summary>
    [DataField]
    public float EnergyExtractMax = 0.01f;

    /// <summary>
    /// Radio channel(s) to send alerts to
    /// </summary>
    [DataField]
    public string[] AlertChannel = ["Engineering", "SyndiEngi"];
}
