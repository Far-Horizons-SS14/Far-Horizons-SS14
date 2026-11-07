using Content.Shared._FarHorizons.Power.SmartGrid;

namespace Content.Server._FarHorizons.Power.SmartGrid;

/// <summary>
/// Availability state for one network. Not a component because networks aren't entities.
/// </summary>
internal sealed class SmartGridNetworkAvailabilityState
{
    /// <summary>
    /// The availability calculated from this network's immediate sources, before upstream propagation.
    /// </summary>
    public SmartGridAvailabilityTracker LocalTracker { get; } = new();

    /// <summary>
    /// The availability including propagated and manually overridden signals. This is the value that should be used for actual availability checks.
    /// </summary>
    public SmartGridPriority EffectiveAvailability = SmartGridPriority.General;

    public void Reset(SmartGridPriority availability)
    {
        LocalTracker.Reset(availability);
        EffectiveAvailability = availability;
    }
}
