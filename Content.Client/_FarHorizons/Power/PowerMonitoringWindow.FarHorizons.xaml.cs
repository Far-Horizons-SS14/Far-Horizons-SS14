using Content.Shared._FarHorizons.Power.SmartGrid;

namespace Content.Client.Power;

public sealed partial class PowerMonitoringWindow
{
    private void UpdateSmartGridStatus(SmartGridNetworkStatus? status)
    {
        SmartGridStatusContainer.Visible = status != null;
        if (status == null)
            return;

        SmartGridStatus.Text = Loc.GetString("power-monitoring-window-smart-grid-status",
            ("availability", GetPriorityName(status.Value.Availability)),
            ("localAvailability", GetPriorityName(status.Value.LocalAvailability)),
            ("sourceHealth", status.Value.SourceConstrained
                ? Loc.GetString("smart-grid-source-health-constrained")
                : Loc.GetString("smart-grid-source-health-healthy")),
            ("transition", Loc.GetString(status.Value.Transition.GetLocalizationKey())));
    }

    private string GetPriorityName(SmartGridPriority priority)
    {
        return Loc.GetString(priority.GetLocalizationKey());
    }
}
