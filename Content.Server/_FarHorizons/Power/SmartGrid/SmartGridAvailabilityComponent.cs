using Robust.Shared.GameObjects;

namespace Content.Server._FarHorizons.Power.SmartGrid;

/// <summary>
/// Server-only automatic availability state owned by a smart-grid generator or primary battery.
/// </summary>
[RegisterComponent]
public sealed partial class SmartGridDeviceAvailabilityComponent : Component
{
    internal SmartGridAvailabilityTracker Tracker { get; } = new();
}
