using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.Power.UI.FusionReactor;

/// <summary>
/// Initializes a <see cref="GasVolumePumpWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class FusionReactorCoolantPumpBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private float _maxTransferRate;

    [ViewVariables]
    private FusionReactorCoolantPumpWindow? _window;

    public FusionReactorCoolantPumpBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<FusionReactorCoolantPumpWindow>();

        if (EntMan.TryGetComponent(Owner, out FusionReactorCoolantPumpComponent? pump))
        {
            _maxTransferRate = pump.FlowRateMax;
        }

        _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
        _window.PumpTransferRateChanged += OnPumpTransferRatePressed;
        Update();
    }

    private void OnToggleStatusButtonPressed(bool status)
    {
        SendPredictedMessage(new FusionReactorCoolantPumpSetEnableMessage(status));
    }

    private void OnPumpTransferRatePressed(string value)
    {
        var rate = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;
        rate = Math.Clamp(rate, 0f, _maxTransferRate);

        SendPredictedMessage(new FusionReactorCoolantPumpSetFlowMessage(rate));
    }

    public override void Update()
    {
        base.Update();

        if (_window is null || !EntMan.TryGetComponent(Owner, out FusionReactorCoolantPumpComponent? pump))
            return;

        _window.Title = Identity.Name(Owner, EntMan);
        _window.SetPumpStatus(pump.Enabled);
        _window.SetTransferRate(pump.FlowRate);
    }
}
