using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.SmartGrid;

/// <summary>
///     Stores an optional manual availability signal for a smart-grid output boundary.
/// </summary>
[RegisterComponent]
public sealed partial class SmartGridControlComponent : Component
{
    /// <summary>
    ///     The availability signal sent to this device's output network, or null for automatic propagation.
    /// </summary>
    [ViewVariables]
    public SmartGridPriority? AvailabilityOverride;
}

/// <summary>
///     Requests that a smart-grid output boundary use an availability signal.
///     Repeating its current override returns the boundary to automatic propagation.
/// </summary>
[Serializable, NetSerializable]
public sealed class SmartGridSetAvailabilityOverrideMessage(SmartGridPriority priority) : BoundUserInterfaceMessage
{
    public SmartGridPriority Priority = priority;
}

/// <summary>
///     Requests that a primary battery toggle the selected local-availability reserve breaker.
///     This changes automatic reserve shedding, not the output availability override.
/// </summary>
[Serializable, NetSerializable]
public sealed class SmartGridToggleLocalAvailabilityMessage(SmartGridPriority priority) : BoundUserInterfaceMessage
{
    public SmartGridPriority Priority = priority;
}
