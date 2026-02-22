using System.Numerics;
using Content.Shared._FarHorizons.Shuttles;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using TurretControlConsoleWindow = Content.Client._FarHorizons.Shuttles.UI.TurretControlConsoleWindow;

namespace Content.Client._FarHorizons.Shuttles.BUI;

// Blatant copy of RadarConsoleBoundUserInterface, ya love to see it
[UsedImplicitly]
public sealed class TurretControlConsoleBoundUserInterface : BoundUserInterface
{

    [ViewVariables]
    private TurretControlConsoleWindow? _window;

    public TurretControlConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TurretControlConsoleWindow>();

        _window.TargetSelected += OnTargetSelected;
        _window.FireButtonPressed += OnFirePressed;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not NavBoundUserInterfaceState cState)
            return;

        _window?.UpdateState(cState.State);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null)
            return;

        if (message is not SpaceRescuePingMessage ping)
            return;

        _window.RescuePing(ping);
    }

    private void OnTargetSelected(Vector2? coordinates)
    {
        if (_window is null ) return;
        SendPredictedMessage(new TurretControlSetTargetMessage(coordinates));
    }

    private void OnFirePressed()
    {
        if (_window is null ) return;
        SendPredictedMessage(new TurretControlFireCannonsMessage());
    }
}