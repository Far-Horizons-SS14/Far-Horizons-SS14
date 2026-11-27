using System.Numerics;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client._FarHorizons.Power.SmartGrid;

/// <summary>
///     Battery charge bar with the smart-grid mode label and clickable reserve bands.
///     Clicking the bar toggles between percent and watt-hours when an energy value is supplied.
/// </summary>
public sealed class SmartGridBatteryMeter : BoxContainer
{
    private readonly Label _mode;
    private readonly ProgressBar _chargeBar;
    private readonly Label _leftStatus;
    private readonly Label _display;
    private readonly Label _rightStatus;
    private readonly List<SmartGridBatteryReserveBand> _bands = new();
    private float _charge;
    private float? _wattHours;
    private bool _showWattHours;

    public event Action<SmartGridPriority>? OnPrioritySelected;

    public SmartGridBatteryMeter()
    {
        Orientation = LayoutOrientation.Vertical;

        _mode = new Label
        {
            HorizontalAlignment = HAlignment.Center,
            MouseFilter = MouseFilterMode.Stop,
            StyleClasses = { "StatusFieldTitle" },
        };
        AddChild(_mode);

        var meter = new LayoutContainer
        {
            SetHeight = 28,
            Margin = new Thickness(0, 4, 0, 0),
        };
        AddChild(meter);

        _chargeBar = new ProgressBar
        {
            MinValue = 0f,
            MaxValue = 1f,
            Value = 0f,
            MouseFilter = MouseFilterMode.Ignore,
        };
        _chargeBar.OnKeyBindDown += OnChargeBarPressed;
        meter.AddChild(_chargeBar);
        LayoutContainer.SetAnchorPreset(_chargeBar, LayoutContainer.LayoutPreset.Wide);

        for (var value = (int) SmartGridPriority.LifeSupport;
             value <= (int) SmartGridPriority.General;
             value++)
        {
            var priority = (SmartGridPriority) value;
            var band = new SmartGridBatteryReserveBand(priority)
            {
                Margin = new Thickness(0, -3),
            };
            LayoutContainer.SetAnchorLeft(band, SmartGridBatteryReserve.GetCutoff(priority));
            LayoutContainer.SetAnchorRight(band, SmartGridBatteryReserveBand.GetRecoveryLevel(priority));
            LayoutContainer.SetAnchorTop(band, LayoutContainer.AnchorBegin);
            LayoutContainer.SetAnchorBottom(band, LayoutContainer.AnchorEnd);
            band.OnPrioritySelected += selected => OnPrioritySelected?.Invoke(selected);
            meter.AddChild(band);
            _bands.Add(band);
        }

        _leftStatus = CreateOverlayLabel(HAlignment.Left);
        _display = CreateOverlayLabel(HAlignment.Center);
        _rightStatus = CreateOverlayLabel(HAlignment.Right);
        foreach (var label in new[] { _leftStatus, _display, _rightStatus })
        {
            meter.AddChild(label);
            LayoutContainer.SetAnchorPreset(label, LayoutContainer.LayoutPreset.Wide);
        }
    }

    /// <param name="charge">Charge fraction, from 0 to 1.</param>
    /// <param name="reserveMode">Whether the battery is in reserve mode, which hides the reserve bands.</param>
    /// <param name="smartGrid">Smart-grid state used to mark tripped reserve bands.</param>
    /// <param name="wattHours">Stored energy. When set, clicking the bar toggles to showing it.</param>
    /// <param name="eta">ETA text, shown on the right while charging and on the left otherwise.</param>
    /// <param name="charging">Whether the battery is currently charging.</param>
    public void Update(
        float charge,
        bool reserveMode,
        SmartGridAvailabilityState smartGrid,
        float? wattHours = null,
        string? eta = null,
        bool charging = false)
    {
        charge = Math.Clamp(charge, 0f, 1f);
        _charge = charge;
        _wattHours = wattHours;
        _chargeBar.Value = charge;
        // Only intercept clicks when there is an energy value to toggle to.
        _chargeBar.MouseFilter = wattHours != null ? MouseFilterMode.Stop : MouseFilterMode.Ignore;
        UpdateDisplay();
        UpdateChargeBarColor(charge);
        _leftStatus.Text = charging ? string.Empty : eta ?? string.Empty;
        _rightStatus.Text = charging ? eta ?? string.Empty : string.Empty;

        _mode.Text = Loc.GetString(
            reserveMode ? "smart-grid-battery-mode-reserve" : "smart-grid-battery-mode-primary");
        _mode.ToolTip = Loc.GetString(
            reserveMode ? "smart-grid-battery-mode-reserve-tooltip" : "smart-grid-battery-mode-primary-tooltip");

        foreach (var band in _bands)
        {
            band.Visible = !reserveMode;
            band.Update(smartGrid.LocalAvailability < band.Priority);
        }
    }

    public void SetAccessEnabled(bool hasAccess)
    {
        foreach (var band in _bands)
        {
            band.Disabled = !hasAccess;
        }
    }

    private static Label CreateOverlayLabel(HAlignment alignment)
    {
        return new Label
        {
            HorizontalAlignment = alignment,
            Margin = new Thickness(4, 0, 4, 0),
            MouseFilter = MouseFilterMode.Ignore,
        };
    }

    private void OnChargeBarPressed(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick || _wattHours == null)
            return;

        _showWattHours = !_showWattHours;
        UpdateDisplay();
        args.Handle();
    }

    private void UpdateDisplay()
    {
        _display.Text = _showWattHours && _wattHours != null
            ? Loc.GetString("smart-grid-battery-meter-energy", ("energy", _wattHours.Value.ToString("N0")))
            : Loc.GetString("smart-grid-battery-meter-percent", ("percent", _charge.ToString("P0")));
    }

    private void UpdateChargeBarColor(float charge)
    {
        const float middleHue = 0.066f;
        const float rightHue = 0.33f;
        var hue = charge <= 0.5f
            ? MathHelper.Lerp(0f, middleHue, charge / 0.5f)
            : MathHelper.Lerp(middleHue, rightHue, (charge - 0.5f) / 0.5f);

        _chargeBar.ForegroundStyleBoxOverride ??= new StyleBoxFlat();
        ((StyleBoxFlat) _chargeBar.ForegroundStyleBoxOverride).BackgroundColor =
            Color.FromHsv(new Vector4(hue, 1f, 0.8f, 1f));
    }
}
