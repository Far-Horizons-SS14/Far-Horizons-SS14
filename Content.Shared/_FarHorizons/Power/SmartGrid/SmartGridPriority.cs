namespace Content.Shared._FarHorizons.Power.SmartGrid;

/// <summary>
///     The service level required for a load to remain enabled during smart-grid shedding.
/// </summary>
public enum SmartGridPriority : byte
{
    EssentialSystems,
    LifeSupport,
    ShuttleOperations,
    Lighting,
    General,
}

public static class SmartGridPriorityExtensions
{
    /// <summary>
    ///     Returns the shared localization key for a priority.
    /// </summary>
    public static string GetLocalizationKey(this SmartGridPriority priority)
    {
        return priority switch
        {
            SmartGridPriority.EssentialSystems => "smart-grid-priority-essential-systems",
            SmartGridPriority.LifeSupport => "smart-grid-priority-life-support",
            SmartGridPriority.ShuttleOperations => "smart-grid-priority-shuttle-operations",
            SmartGridPriority.Lighting => "smart-grid-priority-lighting",
            SmartGridPriority.General => "smart-grid-priority-general",
            _ => priority.ToString(),
        };
    }
}
