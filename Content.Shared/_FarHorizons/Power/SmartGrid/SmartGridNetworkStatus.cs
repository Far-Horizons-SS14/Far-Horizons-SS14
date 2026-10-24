using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.SmartGrid;

/// <summary>
///     Diagnostic smart-grid state for one power network.
/// </summary>
[Serializable, NetSerializable]
public struct SmartGridNetworkStatus
{
    public SmartGridPriority Availability;
    public SmartGridPriority LocalAvailability;
    public bool SourceConstrained;
    public SmartGridTransition Transition;

    public SmartGridNetworkStatus(
        SmartGridPriority availability,
        SmartGridPriority localAvailability,
        bool sourceConstrained,
        SmartGridTransition transition)
    {
        Availability = availability;
        LocalAvailability = localAvailability;
        SourceConstrained = sourceConstrained;
        Transition = transition;
    }
}

public enum SmartGridTransition : byte
{
    None,
    Shedding,
    Recovering,
}

/// <summary>
///     Shared localization helpers for smart-grid transition states.
/// </summary>
public static class SmartGridTransitionExtensions
{
    public static string GetLocalizationKey(this SmartGridTransition transition)
    {
        return transition switch
        {
            SmartGridTransition.Shedding => "smart-grid-transition-shedding",
            SmartGridTransition.Recovering => "smart-grid-transition-recovering",
            _ => "smart-grid-transition-stable",
        };
    }
}
