using Content.Client.UserInterface.Controls;
using Content.Shared._FarHorizons.NPC;
using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._FarHorizons.NPC.UI;

public sealed class SimpleBotConfigurationWindow : DefaultWindow
{
    private readonly SwitchButton _power;
    private readonly SwitchButton _speechEnabled;
    private readonly OptionButton _patrolMode;
    private readonly LineEdit _patrolBeacon;

    public event Action<bool, bool, SimpleBotPatrolMode, string>? OnApply;

    public SimpleBotConfigurationWindow()
    {
        Title = Loc.GetString("simple-bot-configuration-title");
        MinSize = SetSize = new Vector2(350, 210);

        var layout = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(8),
            SeparationOverride = 8,
        };

        _power = new SwitchButton { Text = Loc.GetString("simple-bot-configuration-power") };
        _speechEnabled = new SwitchButton { Text = Loc.GetString("simple-bot-configuration-speech") };
        _patrolMode = new OptionButton();
        _patrolMode.AddItem(Loc.GetString("simple-bot-configuration-patrol-wander"), (int) SimpleBotPatrolMode.Wander);
        _patrolMode.AddItem(Loc.GetString("simple-bot-configuration-patrol-leash"), (int) SimpleBotPatrolMode.Leash);
        _patrolMode.AddItem(Loc.GetString("simple-bot-configuration-patrol-route"), (int) SimpleBotPatrolMode.Patrol);
        _patrolMode.OnItemSelected += args => _patrolMode.SelectId(args.Id);
        _patrolBeacon = new LineEdit
        {
            PlaceHolder = Loc.GetString("simple-bot-configuration-patrol-placeholder"),
        };
        _patrolBeacon.OnTextChanged += args =>
        {
            if (args.Text.Length > 64)
                args.Control.Text = args.Text[..64];
        };

        var controls = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 12,
        };
        controls.AddChild(_power);
        controls.AddChild(_speechEnabled);
        layout.AddChild(controls);
        var patrolMode = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };
        patrolMode.AddChild(new Label { Text = Loc.GetString("simple-bot-configuration-patrol-mode") });
        _patrolMode.HorizontalExpand = true;
        patrolMode.AddChild(_patrolMode);
        layout.AddChild(patrolMode);

        var patrolBeacon = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };
        patrolBeacon.AddChild(new Label { Text = Loc.GetString("simple-bot-configuration-patrol") });
        _patrolBeacon.HorizontalExpand = true;
        patrolBeacon.AddChild(_patrolBeacon);
        layout.AddChild(patrolBeacon);

        var apply = new Button
        {
            Text = Loc.GetString("simple-bot-configuration-apply"),
            HorizontalAlignment = HAlignment.Center,
        };
        apply.OnPressed += _ => OnApply?.Invoke(_power.Pressed, _speechEnabled.Pressed, (SimpleBotPatrolMode) _patrolMode.SelectedId, _patrolBeacon.Text);
        layout.AddChild(apply);

        ContentsContainer.AddChild(layout);
    }

    public void SetConfiguration(bool powered, bool speechEnabled, SimpleBotPatrolMode patrolMode, string patrolBeacon)
    {
        _power.Pressed = powered;
        _speechEnabled.Pressed = speechEnabled;
        _patrolMode.SelectId((int) patrolMode);
        _patrolBeacon.Text = patrolBeacon;
    }
}
