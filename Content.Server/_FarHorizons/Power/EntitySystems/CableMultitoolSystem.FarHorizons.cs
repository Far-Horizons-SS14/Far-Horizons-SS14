using System.Linq;
using Content.Server._FarHorizons.Power.SmartGrid;
using Content.Server.Power.NodeGroups;
using Content.Shared._FarHorizons.Power.SmartGrid;

namespace Content.Server.Power.EntitySystems;

public sealed partial class CableMultitoolSystem
{
    [Dependency] private SmartGridSystem _smartGrid = default!;

    private string GetSmartGridStatus(IBasePowerNet network)
    {
        if (!_smartGrid.TryGetStatus(network, out var status))
            return Loc.GetString("cable-multitool-system-smart-grid-unavailable");

        return Loc.GetString("cable-multitool-system-smart-grid",
            ("availability", GetPriorityName(status.Availability)),
            ("localAvailability", GetPriorityName(status.LocalAvailability)),
            ("sourceHealth", status.SourceConstrained
                ? Loc.GetString("smart-grid-source-health-constrained")
                : Loc.GetString("smart-grid-source-health-healthy")),
            ("transition", Loc.GetString(status.Transition.GetLocalizationKey())));
    }

    private string GetPriorityName(SmartGridPriority priority)
    {
        return Loc.GetString(priority.GetLocalizationKey());
    }

    private string GetSmartGridSources(IBasePowerNet network)
    {
        var sources = _smartGrid.GetImmediateSources(network)
            .Select(source => source.CurrentStorage is { } storage && source.MaximumStorage is { } capacity
                ? Loc.GetString("cable-multitool-system-smart-grid-battery-source",
                    ("name", MetaData(source.Owner).EntityName),
                    ("output", source.CurrentOutput),
                    ("maximumOutput", source.MaximumOutput),
                    ("storage", storage),
                    ("capacity", capacity),
                    ("health", GetSourceHealthName(source.Health)))
                : Loc.GetString("cable-multitool-system-smart-grid-source",
                    ("name", MetaData(source.Owner).EntityName),
                    ("output", source.CurrentOutput),
                    ("maximumOutput", source.MaximumOutput),
                    ("health", GetSourceHealthName(source.Health))))
            .ToArray();
        return Loc.GetString("cable-multitool-system-smart-grid-sources",
            ("sources", sources.Length == 0
                ? Loc.GetString("cable-multitool-system-smart-grid-no-sources")
                : string.Join("\n", sources)));
    }

    private string GetSourceHealthName(SmartGridSourceHealth health)
    {
        return health switch
        {
            SmartGridSourceHealth.OutputCapped => Loc.GetString("smart-grid-source-health-output-capped"),
            SmartGridSourceHealth.LowReserve => Loc.GetString("smart-grid-source-health-low-reserve"),
            SmartGridSourceHealth.Disabled => Loc.GetString("smart-grid-source-health-disabled"),
            _ => Loc.GetString("smart-grid-source-health-healthy"),
        };
    }
}
