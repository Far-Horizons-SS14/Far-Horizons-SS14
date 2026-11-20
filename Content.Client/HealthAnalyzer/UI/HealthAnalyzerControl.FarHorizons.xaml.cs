using System.Linq;
using Content.Client.Message;
using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerControl
{
    private void DrawStatuses(List<string> statuses)
    {
        LimbStatusContainer.RemoveAllChildren();
        LimbStatusContainer.Visible = false;
        LimbStatusDivider.Visible = false;

        if (!statuses.Any())
            return;

        LimbStatusContainer.Visible = true;
        LimbStatusDivider.Visible = true;

        LimbStatusContainer.AddChild(new Label
        {
            Text = Loc.GetString("health-analyzer-window-status-header"),
            StyleClasses = { "LabelSubText" },
            Margin = new Thickness(0, 0, 0, 2),
        });

        foreach (var status in statuses.OrderBy(s => s))
        {
            var row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Margin = new Thickness(0, 2),
            };

            var accent = new PanelContainer
            {
                MinWidth = 4,
                MinHeight = 16,
                Margin = new Thickness(0, 0, 6, 0),
                PanelOverride = new StyleBoxFlat(Color.Gold),
            };

            var label = new Label
            {
                Text = status,
                HorizontalExpand = true,
                HorizontalAlignment = HAlignment.Left,
            };

            row.AddChild(accent);
            row.AddChild(label);
            LimbStatusContainer.AddChild(row);
        }
    }

    private void DrawAttention(List<string> limbNames)
    {
        var hasLimbs = limbNames.Count > 0;

        AttentionContainer.Visible = hasLimbs;
        AttentionDivider.Visible = hasLimbs;

        if (!hasLimbs)
            return;

        var joined = string.Join(", ", limbNames.Select(FormattedMessage.EscapeText));
        AttentionLabel.SetMarkup($"[color=#ff6b6b]{joined}[/color]");
    }

    private List<string> GetDetectableStatusNames(IEnumerable<ProtoId<LimbStatusEffectPrototype>> ids)
    {
        var protos = ids
                     .Where(p => _prototypes.TryIndex(p, out var proto) && proto.Detectable && proto.Name != null)
                     .Select(p => _prototypes.Index(p))
                     .ToList();

        return _limbStatusEffect
               .GetDetectable(protos)
               .Select(p => Loc.GetString(p.Name!))
               .ToList();
    }
}