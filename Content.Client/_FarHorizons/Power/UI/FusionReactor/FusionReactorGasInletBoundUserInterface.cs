using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared.IdentityManagement;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.Power.UI.FusionReactor;
/// <summary>
/// Initializes a <see cref="GasVolumePumpWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class FusionReactorGasInletBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private float _maxTransferRate;

    [ViewVariables]
    private FusionReactorGasInletWindow? _window;

    public FusionReactorGasInletBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<FusionReactorGasInletWindow>();

        _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
        _window.PowerUseChanged += OnPumpTransferRatePressed;
        _window.SetEntity(Owner);
        Update();
    }

    private void OnToggleStatusButtonPressed(bool status)
    {
        SendPredictedMessage(new FusionReactorGasInletSetEnableMessage(status));
    }

    private void OnPumpTransferRatePressed(string value)
    {
        var rate = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;
        rate = Math.Clamp(rate, 0f, _maxTransferRate);

        SendPredictedMessage(new FusionReactorGasInletSetPowerMessage(rate));
    }

    public override void Update()
    {
        base.Update();

        _window?.Title = Identity.Name(Owner, EntMan);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not FusionReactorGasInletBuiState inletState)
            return;

        _maxTransferRate = inletState.MaxPowerSetting;
        _window?.SetPumpStatus(inletState.Enabled);
        _window?.SetTransferRate(inletState.PowerSetting);
        _window?.SetMaxTransferRate(inletState.MaxPowerSetting);
        _window?.Update();
    }
}
