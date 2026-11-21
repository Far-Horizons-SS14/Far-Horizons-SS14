using System.Linq;
using Content.Client._FarHorizons.LimbDamage.UI;
using Content.Client.Message;
using Content.Shared._FarHorizons.CCVar;
using Content.Shared._FarHorizons.LimbDamage;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Research.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerControl
{
    private readonly IConfigurationManager _cfg;
    private readonly LimbDamageSystem _limbDamage;
    private readonly LimbStatusEffectSystem _limbStatusEffect;
    private readonly LimbTargettingSystem _limbTargetting;
    public event Action<BaseButton.ButtonEventArgs>? OnServerListButtonPressed;
    public event Action<ProtoId<OrganCategoryPrototype>>? OnLimbSelectionChanged;

    public ProtoId<OrganCategoryPrototype> CurTarget { get; private set; } = "Torso";
    private static ProtoId<OrganCategoryPrototype> _defaultLimb = "Torso";

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

    private void SyncToLimbTarget(ProtoId<OrganCategoryPrototype> target)
    {
        if (!_cfg.GetCVar(FHCCVars.LimbTargettingSyncHealthAnalyzer))
            return;
        
        _limbTargetting.SetTarget(target);
    }

    private void SyncFromLimbTarget(ProtoId<OrganCategoryPrototype> target)
    {
        if (Disposed)
            _limbTargetting.LocalTargetUpdated -= SyncFromLimbTarget;

        CurTarget = target;
        LimbTargettingDisplay.UpdateLimb(CurTarget);
        Repopulate();
        OnLimbSelectionChanged?.Invoke(CurTarget);
    }

    private void SetupSyncLimbTarget(bool value)
    {
        if (!value)
        {
            _limbTargetting.LocalTargetUpdated -= SyncFromLimbTarget;
            return;
        }
        
         _limbTargetting.LocalTargetUpdated += SyncFromLimbTarget;
         SyncFromLimbTarget(_limbTargetting.GetTarget() ?? _defaultLimb);
         OnLimbSelectionChanged?.Invoke(CurTarget);
    }

    public void SetEntity(EntityUid uid)
    {
        if (!_entityManager.HasComponent<ResearchClientComponent>(uid))
        {
            ServerListButton.Visible = false;
        }
    }

    private void SetLimbTarget(ProtoId<OrganCategoryPrototype> target)
    {
        SyncToLimbTarget(target);

        if (target == CurTarget)
            return;

        CurTarget = target;
        LimbTargettingDisplay.UpdateLimb(CurTarget);
        Repopulate();
    }

    private void Repopulate()
    {
        if (_targetEnt == null)
            return;

        if (!_entityManager.HasComponent<LimbDamageableComponent>(_targetEnt.Value))
            LimbTargettingDisplay.Visible = false;

        var fullBodyDamage = _limbDamage.TryGetFullBodyDamage(_targetEnt.Value);
        var fullBodyDamageGroups = _limbDamage.TryGetFullBodyDamageGroups(_targetEnt.Value);

        if (fullBodyDamage == null ||
            fullBodyDamageGroups == null)
            return;

        if (!fullBodyDamage.ContainsKey(CurTarget))
            CurTarget = _defaultLimb;

        LimbTargettingDisplay.SetHealth(fullBodyDamage.ToDictionary(p => p.Key, p => p.Value.Sum(e => (float)e.Value)));

        // Total Damage

        DamageLabel.Text = fullBodyDamage[CurTarget].Select(p => (float)p.Value).Sum().ToString();
        
        // Damage Groups

        IReadOnlyDictionary<ProtoId<DamageTypePrototype>, FixedPoint2> damagePerType = fullBodyDamage[CurTarget];

        var groupOrder = new List<string> { "Burn", "Brute", "Airloss", "Toxin", "Genetic" };
        var sortedGroups = fullBodyDamageGroups[CurTarget]
            .OrderBy(g => groupOrder.IndexOf(g.Key))
            .ToDictionary(g => g.Key, g => g.Value);

        DrawDiagnosticGroups(sortedGroups, damagePerType);

        // Statuses

        var statuses = _limbStatusEffect.GetStatusPerLimb(_targetEnt.Value);
        var detectablePerLimb = statuses
                                .Select(kv => (Limb: kv.Key, Names: GetDetectableStatusNames(kv.Value)))
                                .Where(p => p.Names.Count > 0)
                                .ToList();
        
        var statusNames = detectablePerLimb.FirstOrDefault(p => p.Limb.Equals(CurTarget)).Names ?? new List<string>();
        var limbNames = detectablePerLimb.Select(p => Loc.GetString($"limb-category-limb-name-{p.Limb.Id.ToLower()}")).ToList();
        
        DrawStatuses(statusNames);
        DrawAttention(limbNames);

        StatusText.Visible = CurTarget == _defaultLimb;
        StatusLabel.Visible = CurTarget == _defaultLimb;
        TemperatureText.Visible = CurTarget == _defaultLimb;
        TemperatureLabel.Visible = CurTarget == _defaultLimb;
        BloodText.Visible = CurTarget == _defaultLimb;
        BloodLabel.Visible = CurTarget == _defaultLimb;
        DiseaseText.Visible = CurTarget == _defaultLimb;
        DiseaseContainer.Visible = CurTarget == _defaultLimb;
        ChemicalsContainer.Visible = CurTarget == _defaultLimb;
    }

    protected override void Deparented() => 
        _limbTargetting.LocalTargetUpdated -= SyncFromLimbTarget;
}