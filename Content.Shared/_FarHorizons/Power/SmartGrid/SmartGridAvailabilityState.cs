using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.SmartGrid;

/// <summary>
///     Smart-grid availability data presented by a power-device UI.
/// </summary>
/// <param name="InputAvailability">The signal received from the input network, if it has a source.</param>
/// <param name="EffectiveAvailability">The signal currently sent to the output network.</param>
/// <param name="LocalAvailability">The output network's automatic availability before upstream constraints.</param>
/// <param name="AvailabilityOverride">The manual output signal, or null while operating automatically.</param>
/// <param name="InputIsBatteryBacked">Whether the input network has an enabled battery output.</param>
[Serializable, NetSerializable]
public readonly record struct SmartGridAvailabilityState(
    SmartGridPriority? InputAvailability,
    SmartGridPriority EffectiveAvailability,
    SmartGridPriority LocalAvailability,
    SmartGridPriority? AvailabilityOverride,
    bool InputIsBatteryBacked = false);
