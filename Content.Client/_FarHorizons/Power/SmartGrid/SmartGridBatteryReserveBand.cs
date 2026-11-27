using System.Numerics;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client._FarHorizons.Power.SmartGrid;

/// <summary>
///     Dashed outline over a <see cref="SmartGridBatteryMeter"/> marking the charge range between where a primary
///     battery sheds a priority and where it recovers it. Clicking it selects that priority for the battery.
/// </summary>
public sealed class SmartGridBatteryReserveBand : Control
{
    private const float DashLength = 4f;
    private const float GapLength = 3f;
    private const float DisabledAlpha = 0.5f;

    private readonly string _description;
    private bool _tripped;
    private bool _hovered;
    private bool _disabled;

    public event Action<SmartGridPriority>? OnPrioritySelected;

    public SmartGridPriority Priority { get; }

    /// <summary>
    ///     Disabled bands are drawn dimmed, ignore clicks, and explain the missing access in their tooltip.
    /// </summary>
    public bool Disabled
    {
        get => _disabled;
        set
        {
            if (_disabled == value)
                return;

            _disabled = value;
            UpdateToolTip();
        }
    }

    public SmartGridBatteryReserveBand(SmartGridPriority priority)
    {
        Priority = priority;
        MinHeight = 28;
        HorizontalExpand = true;
        VerticalExpand = true;
        // Stop rather than Ignore, so the tooltip still shows while disabled.
        MouseFilter = MouseFilterMode.Stop;
        _description = Loc.GetString(
            "smart-grid-battery-band-tooltip",
            ("priority", SmartGridAvailabilityControl.GetPriorityName(priority)),
            ("shed", SmartGridBatteryReserve.GetCutoff(priority).ToString("P0")),
            ("recovery", GetRecoveryLevel(priority).ToString("P0")));
        UpdateToolTip();

        OnMouseEntered += _ => _hovered = true;
        OnMouseExited += _ => _hovered = false;
        OnKeyBindDown += OnPressed;
    }

    public static float GetRecoveryLevel(SmartGridPriority priority)
    {
        return Math.Min(1f, SmartGridBatteryReserve.GetCutoff(priority) + SmartGridBatteryReserve.RecoveryMargin);
    }

    public void Update(bool tripped)
    {
        _tripped = tripped;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var box = PixelSizeBox;
        var color = _tripped ? Color.Red : Color.Green;
        if (_disabled)
            color = color.WithAlpha(DisabledAlpha);

        var thickness = _hovered && !_disabled ? 2f : 1f;
        for (var inset = 0f; inset < thickness; inset++)
        {
            DrawDashedLine(handle, new(box.Left + inset, box.Top + inset), new(box.Right - inset, box.Top + inset), color);
            DrawDashedLine(handle, new(box.Left + inset, box.Bottom - inset), new(box.Right - inset, box.Bottom - inset), color);
            DrawDashedLine(handle, new(box.Left + inset, box.Top + inset), new(box.Left + inset, box.Bottom - inset), color);
            DrawDashedLine(handle, new(box.Right - inset, box.Top + inset), new(box.Right - inset, box.Bottom - inset), color);
        }
    }

    private void UpdateToolTip()
    {
        ToolTip = _disabled
            ? Loc.GetString("smart-grid-battery-band-tooltip-insufficient-access", ("details", _description))
            : _description;
    }

    private void OnPressed(GUIBoundKeyEventArgs args)
    {
        if (_disabled || args.Function != EngineKeyFunctions.UIClick)
            return;

        OnPrioritySelected?.Invoke(Priority);
        args.Handle();
    }

    private static void DrawDashedLine(DrawingHandleScreen handle, Vector2 start, Vector2 end, Color color)
    {
        var direction = end - start;
        var length = direction.Length();
        if (length <= 0f)
            return;

        direction /= length;
        for (var offset = 0f; offset < length; offset += DashLength + GapLength)
        {
            handle.DrawLine(
                start + direction * offset,
                start + direction * Math.Min(offset + DashLength, length),
                color);
        }
    }
}
