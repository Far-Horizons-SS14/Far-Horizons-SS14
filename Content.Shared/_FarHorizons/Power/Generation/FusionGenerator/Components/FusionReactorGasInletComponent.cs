using Content.Shared._FarHorizons.Fusion;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FusionReactorGasInletComponent : Component
{
    /// <summary>
    /// The number of watts the gas inlet is allowed to use to convert gasses
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float PowerSetting = 4000;

    /// <summary>
    /// The max number of watts the gas inlet is allowed to use to convert gasses
    /// </summary>
    [DataField]
    public float MaxPowerSetting = 1000000;

    /// <summary>
    /// If the inlet should be processing
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = false;

    /// <summary>
    /// Name of the pipe node
    /// </summary>
    [DataField]
    public string PipeName = "pipe";

    /// <summary>
    /// The mol/s currently being produced 
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public List<(FusionAtom, double)> Production = [];
}