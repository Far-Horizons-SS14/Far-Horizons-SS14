using System.Numerics;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._FarHorizons.Power.SmartGrid;

/// <summary>
///     Reusable selector and signal display for a smart-grid output boundary.
/// </summary>
public sealed class SmartGridAvailabilityControl : BoxContainer
{
    private readonly Dictionary<SmartGridPriority, Label> _inputMarkers = new();
    private readonly Dictionary<SmartGridPriority, Label> _outputMarkers = new();
    private readonly Dictionary<SmartGridPriority, SmartGridPriorityButton> _buttons = new();

    public event Action<SmartGridPriority>? OnPrioritySelected;

    public SmartGridAvailabilityControl()
    {
        Orientation = LayoutOrientation.Vertical;
        AddChild(new Label
        {
            Text = Loc.GetString("smart-grid-availability-header"),
            ToolTip = Loc.GetString("smart-grid-availability-markers"),
            StyleClasses = { "LabelKeyText" },
        });

        for (var value = (int) SmartGridPriority.General; value >= (int) SmartGridPriority.EssentialSystems; value--)
        {
            var priority = (SmartGridPriority) value;
            var row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                MinHeight = 22,
            };

            var button = new SmartGridPriorityButton
            {
                HorizontalExpand = true,
                ToolTip = GetPriorityTooltip(priority),
            };
            var label = new Label
            {
                Text = GetPriorityName(priority),
                FontColorOverride = Color.FromHsv(new Vector4((float) priority / (float) SmartGridPriority.General * 0.33f, 1f, 1f, 1f)),
                HorizontalAlignment = Control.HAlignment.Center,
                VerticalAlignment = Control.VAlignment.Center,
                MouseFilter = Control.MouseFilterMode.Ignore,
            };
            var outputMarker = new Label
            {
                MinWidth = 20,
                HorizontalAlignment = Control.HAlignment.Center,
                VerticalAlignment = Control.VAlignment.Center,
            };
            var inputMarker = new Label
            {
                MinWidth = 20,
                HorizontalAlignment = Control.HAlignment.Center,
                VerticalAlignment = Control.VAlignment.Center,
            };
            button.OnPressed += _ => OnPrioritySelected?.Invoke(priority);

            var priorityControl = new LayoutContainer
            {
                HorizontalExpand = true,
            };
            priorityControl.AddChild(button);
            priorityControl.AddChild(label);
            LayoutContainer.SetAnchorPreset(button, LayoutContainer.LayoutPreset.Wide);
            LayoutContainer.SetAnchorPreset(label, LayoutContainer.LayoutPreset.Wide);

            row.AddChild(inputMarker);
            row.AddChild(priorityControl);
            row.AddChild(outputMarker);
            AddChild(row);
            _inputMarkers.Add(priority, inputMarker);
            _outputMarkers.Add(priority, outputMarker);
            _buttons.Add(priority, button);
        }
    }

    public void Update(SmartGridAvailabilityState state)
    {
        foreach (var (priority, inputMarker) in _inputMarkers)
        {
            var isInput = state.InputAvailability == priority;
            inputMarker.Text = isInput ? "->" : string.Empty;
            inputMarker.ToolTip = isInput
                ? Loc.GetString("smart-grid-availability-input")
                : null;

            var outputMarker = _outputMarkers[priority];
            var isOutput = state.EffectiveAvailability == priority;
            outputMarker.Text = isOutput ? "->" : string.Empty;
            outputMarker.ToolTip = isOutput
                ? Loc.GetString("smart-grid-availability-output")
                : null;

            var button = _buttons[priority];
            button.SolidOutline = state.AvailabilityOverride == priority;
        }
    }

    public void SetAccessEnabled(bool hasAccess)
    {
        foreach (var (priority, button) in _buttons)
        {
            button.Disabled = !hasAccess;
            button.ToolTip = hasAccess
                ? GetPriorityTooltip(priority)
                : Loc.GetString("smart-grid-availability-insufficient-access");
        }
    }

    public static string GetPriorityName(SmartGridPriority priority)
    {
        return Loc.GetString(priority.GetLocalizationKey());
    }

    private static string GetPriorityTooltip(SmartGridPriority priority)
    {
        return priority switch
        {
            SmartGridPriority.EssentialSystems => Loc.GetString("smart-grid-availability-select-essential-systems"),
            SmartGridPriority.LifeSupport => Loc.GetString("smart-grid-availability-select-life-support"),
            SmartGridPriority.ShuttleOperations => Loc.GetString("smart-grid-availability-select-shuttle-operations"),
            SmartGridPriority.Lighting => Loc.GetString("smart-grid-availability-select-lighting"),
            SmartGridPriority.General => Loc.GetString("smart-grid-availability-select-general"),
            _ => priority.ToString(),
        };
    }
}

/// <summary>
///     Draws smart-grid output and override markers without changing the standard button interaction behavior.
/// </summary>
internal sealed class SmartGridPriorityButton : Button
{
    private const int OutlineWidth = 3;
    private static readonly Color MarkerColor = Color.Cyan;

    public bool SolidOutline;

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (SolidOutline)
            DrawSolidOutline(handle, PixelSizeBox);
    }

    private static void DrawSolidOutline(DrawingHandleScreen handle, UIBox2 box)
    {
        for (var inset = 1; inset <= OutlineWidth; inset++)
        {
            handle.DrawRect(
                new UIBox2(box.Left + inset, box.Top + inset, box.Right - inset, box.Bottom - inset),
                MarkerColor,
                false);
        }
    }
}
