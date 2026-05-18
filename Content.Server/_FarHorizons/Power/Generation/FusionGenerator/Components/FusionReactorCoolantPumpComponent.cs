using Content.Shared.Atmos;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent]
public sealed partial class FusionReactorCoolantPumpComponent : Component
{
    [DataField]
    public bool IsInlet = true;

    /// <summary>
    /// Name of the pipe node
    /// </summary>
    [DataField]
    public string PipeName = "pipe";
    
    [DataField]
    public float FlowRate = Atmospherics.MaxTransferRate;

    /// <summary>
    /// Maximum volume of gas to process per tick
    /// </summary>
    [DataField]
    public float FlowRateMax = Atmospherics.MaxTransferRate * 5;
}