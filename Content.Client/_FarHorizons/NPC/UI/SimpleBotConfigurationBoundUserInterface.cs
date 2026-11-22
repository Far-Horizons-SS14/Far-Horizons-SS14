using Content.Shared._FarHorizons.NPC;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.NPC.UI;

public sealed class SimpleBotConfigurationBoundUserInterface : BoundUserInterface
{
    private SimpleBotConfigurationWindow? _window;

    public SimpleBotConfigurationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<SimpleBotConfigurationWindow>();
        _window.OnApply += OnApply;
        Refresh();
    }

    public void Refresh()
    {
        if (_window == null ||
            !EntMan.TryGetComponent(Owner, out SimpleBotConfigurationComponent? configuration))
        {
            return;
        }

        _window.SetConfiguration(configuration.Powered, configuration.SpeechEnabled, configuration.PatrolMode, configuration.PatrolBeacon);
    }

    private void OnApply(bool powered, bool speechEnabled, SimpleBotPatrolMode patrolMode, string patrolBeacon)
    {
        SendMessage(new SimpleBotConfigurationBuiMessage(powered, speechEnabled, patrolMode, patrolBeacon));
    }
}
