namespace Content.Server._FarHorizons.Power.SmartGrid;

/// <summary>
///     Diagnostic output and reserve status for an immediate smart-grid source.
/// </summary>
public readonly record struct SmartGridSourceStatus(
    EntityUid Owner,
    float CurrentOutput,
    float MaximumOutput,
    float? CurrentStorage,
    float? MaximumStorage,
    SmartGridSourceHealth Health);

/// <summary>
///     Health classification for an immediate smart-grid source.
/// </summary>
public enum SmartGridSourceHealth : byte
{
    Healthy,
    OutputCapped,
    LowReserve,
    Disabled,
}
