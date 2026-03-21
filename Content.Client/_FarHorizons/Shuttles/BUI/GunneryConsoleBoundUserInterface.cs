using System.Numerics;
using Content.Client._FarHorizons.Shuttles.UI;
using Content.Shared._FarHorizons.Shuttles;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Map;

namespace Content.Client._FarHorizons.Shuttles.BUI;

[UsedImplicitly]
public sealed class GunneryConsoleBoundUserInterface : BoundUserInterface
{
    
    [ViewVariables]
    private GunneryConsoleWindow? _window;

    public GunneryConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GunneryConsoleWindow>();
        _window.FireButtonPressed += OnFireButtonPressed;
        _window.TargetMessage += OnTargetMessage;
        _window.ButtonUpdate += OnButtonUpdate;

        _window.SetEntity(Owner);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        switch (state)
        {
            case GunneryConsoleBuiState gunState:
                _window?.SetShuttle(EntMan.GetCoordinates(gunState.State.Coordinates)?.EntityId);
                _window?.Update(gunState);
                _window?.UpdateState(gunState.State);
                break;
            default: 
                return;
        }
    }

    private void OnFireButtonPressed(NetCoordinates position, List<NetEntity> turretEntities) 
        => SendMessage(new GunneryConsoleFireActionMessage(position, turretEntities));

    private void OnTargetMessage(Vector2? position) 
        => SendMessage(new GunneryConsoleTargetActionMessage(position));

    private void OnButtonUpdate(List<(NetEntity, bool)> selections) 
        => SendMessage(new GunneryConsoleSelectActionMessage(selections));
    
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null)
            return;

        if (message is not BulletTracerPingMessage ping)
            return;

        _window.TracerPing(ping);
    }
}
