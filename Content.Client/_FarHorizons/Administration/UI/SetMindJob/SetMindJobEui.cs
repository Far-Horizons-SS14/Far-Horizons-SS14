using Content.Client.Eui;
using Content.Shared._FarHorizons.Administration.SetMindJob;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._FarHorizons.Administration.UI.SetMindJob;

[UsedImplicitly]
public sealed class SetMindJobEui : BaseEui
{
    private readonly SetMindJobMenu _window;
    private IEntityManager _entManager;

    public SetMindJobEui()
    {
        _entManager = IoCManager.Resolve<IEntityManager>();
        _window = new SetMindJobMenu();
        _window.OnClose += OnClosed;
    }

    private void OnClosed()
    {
        SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        var windowState = (SetMindJobEuiState) state;
        _window.TargetEntityId = windowState.TargetNetUserId;

    }
}
