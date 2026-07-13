using Content.Shared.Atmos;

namespace Content.Shared._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent]
public sealed partial class FusionReactorCoolantPumpComponent : Component
{
    /// <summary>
    /// If the coolant pump is an inlet or an outlet
    /// </summary>
    [DataField]
    public bool IsInlet = true;

    /// <summary>
    /// If the coolant pump is active
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = false;

    /// <summary>
    /// Name of the pipe node
    /// </summary>
    [DataField]
    public string PipeName = "pipe";

    /// <summary>
    /// Volume of gas to process per tick
    /// </summary>
    [DataField]
    public float FlowRate = Atmospherics.MaxTransferRate;

    /// <summary>
    /// Maximum volume of gas to process per tick
    /// </summary>
    [DataField]
    public float FlowRateMax = Atmospherics.MaxTransferRate * 5;

    /// <summary>
    /// Power in watts required for the pump to function
    /// </summary>
    [DataField]
    public float PowerDraw = 200;
}