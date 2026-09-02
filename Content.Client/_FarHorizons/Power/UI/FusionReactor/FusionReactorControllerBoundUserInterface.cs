using Content.Client.UserInterface;
using Content.Shared._FarHorizons.Fusion;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared.IdentityManagement;
using JetBrains.Annotations;
using Robust.Client.Timing;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.Power.UI.FusionReactor;

[UsedImplicitly]
public sealed class FusionReactorControllerBoundUserInterface : BoundUserInterface, IBuiPreTickUpdate
{
    [Dependency] private readonly IClientGameTiming _gameTiming = null!;

    [ViewVariables]
    private FusionReactorControllerWindow? _window;

    private bool _master = false;
    private BuiPredictionState? _pred;

    public FusionReactorControllerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _pred = new BuiPredictionState(this, _gameTiming);

        _window = this.CreateWindow<FusionReactorControllerWindow>();

        _window.SetEntity(Owner, EntMan);

        _window.OnTransferSet += OnInjectionSet;
        _window.OnExtractSet += TrySendMessage;
        _window.OnPressureSet += OnPressureSet;

        _window.OnMaserSetInject += TrySendMessage;

        _window.OnEditInject += TrySendMessage;

        _window.EjectPressed += OnEjectPressed;

        Update();
    }

    void IBuiPreTickUpdate.PreTickUpdate()
    {
        var messages = _window?.GetMaserCoalescers();

        if (messages == null || messages.Count <= 0)
            return;

        foreach (var message in messages)
        {
            _pred!.SendMessage(message);
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not FusionReactorControllerBuiState controllerState)
            return;

        foreach (var replayMsg in _pred!.MessagesToReplay())
        {
            switch (replayMsg)
            {
                case FusionReactorControllerSetMaserPowerMessage maserPowerMessage:
                    if (!controllerState.Masers.TryGetValue(maserPowerMessage.Maser, out var maserBuiState))
                        break;
                    maserBuiState.PowerSetting = maserPowerMessage.Message.PowerLevel;
                    break;
            }
        }

        _master = controllerState.IsMaster;
        _window?.UpdateState(controllerState);
    }

    private void OnInjectionSet(KeyValuePair<FusionAtom, FusionReactorTransferData> injection) =>
        TrySendMessage(new FusionReactorControllerSetInjectMessage(injection.Key, injection.Value));

    private void OnPressureSet(float pressure) =>
        TrySendMessage(new FusionReactorControllerSetPressureMessage(Math.Max(pressure, 0)));

    private void OnEjectPressed() =>
        TrySendMessage(new FusionReactorControllerEjectMessage());

    private void TrySendMessage(BoundUserInterfaceMessage message)
    {
        if (!_master)
            return;

        SendMessage(message);
    }
}