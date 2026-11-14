using Content.Shared._FarHorizons.Power.SmartGrid;

namespace Content.Server._FarHorizons.Power.SmartGrid;

/// <summary>
///     Marks an APC receiver or direct power consumer as participating in smart-grid load shedding.
/// </summary>
[RegisterComponent]
public sealed partial class SmartGridLoadComponent : Component
{
    /// <summary>
    ///     The minimum smart-grid service level required to keep this load enabled.
    /// </summary>
    [DataField("priority")]
    public SmartGridPriority Priority = SmartGridPriority.General;

    /// <summary>
    ///     Whether the smart grid has disabled this load. This is independent of manual power controls.
    /// </summary>
    [ViewVariables]
    public bool Shed;

    /// <summary>
    ///     Direct consumers do not have a shared manual-disable state, so preserve their previous solver state while shed.
    /// </summary>
    public bool? DirectConsumerEnabledBeforeShed;
}
