namespace Content.Shared._FarHorizons.Power.SmartGrid;

/// <summary>
///     Shared battery reserve thresholds for each smart-grid priority.
/// </summary>
public static class SmartGridBatteryReserve
{
    /// <summary>
    /// How much above each cutoff the battery must gain to recover to a given priority.
    /// </summary>
    public const float RecoveryMargin = 0.1f;

    /// <summary>
    /// The threshold from 0->1 at which a given availability level will be shed.
    /// </summary>
    public static float GetCutoff(SmartGridPriority priority)
    {
        return priority switch
        {
            SmartGridPriority.EssentialSystems => 0f,
            SmartGridPriority.LifeSupport => 0.2f,
            SmartGridPriority.ShuttleOperations => 0.4f,
            SmartGridPriority.Lighting => 0.6f,
            SmartGridPriority.General => 0.8f,
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
        };
    }

    public static float GetRecovery(SmartGridPriority priority)
    {
        return Math.Min(1f, GetCutoff(priority) + RecoveryMargin);
    }
}
