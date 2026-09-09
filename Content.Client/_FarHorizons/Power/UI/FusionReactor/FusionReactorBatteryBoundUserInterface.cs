using Content.Client.UserInterface;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using Content.Shared.IdentityManagement;
using JetBrains.Annotations;
using Robust.Client.Timing;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.Power.UI.FusionReactor;

[UsedImplicitly]
public sealed class FusionReactorCapacitorBoundUserInterface : BoundUserInterface, IBuiPreTickUpdate
{
    [Dependency] private readonly IClientGameTiming _gameTiming = null!;

    [ViewVariables]
    private FusionReactorBatteryWindow? _window;

    private BuiPredictionState? _pred;
    private InputCoalescer<float> _powerLevelCoalescer;

    public FusionReactorCapacitorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) => IoCManager.InjectDependencies(this);

    protected override void Open()
    {
        base.Open();

        _pred = new BuiPredictionState(this, _gameTiming);

        _window = this.CreateWindow<FusionReactorBatteryWindow>();

        _window.SetEntity(Owner, EntMan);

        _window.SetChargeRate += val => _powerLevelCoalescer.Set(val);

        _window.SetCanCharge += val => _pred.SendMessage(new FusionReactorBatterySetCanChargeMessage(val));
        _window.SetCanDischarge += val => _pred.SendMessage(new FusionReactorBatterySetCanDischargeMessage(val));

        Update();
    }

    void IBuiPreTickUpdate.PreTickUpdate()
    {
        if (_powerLevelCoalescer.CheckIsModified(out var powerLevelValue))
            _pred!.SendMessage(new FusionReactorBatterySetMaxInputMessage(powerLevelValue));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not FusionReactorBatteryBuiState batteryState)
            return;

        foreach (var replayMsg in _pred!.MessagesToReplay())
        {
            switch (replayMsg)
            {
                case FusionReactorBatterySetMaxInputMessage maxInputMessage:
                    batteryState.MaxExternalInput = maxInputMessage.MaxExternalInput;
                    break;

                case FusionReactorBatterySetCanChargeMessage canChargeMessage:
                    batteryState.CanCharge = canChargeMessage.CanCharge;
                    break;

                case FusionReactorBatterySetCanDischargeMessage canDischargeMessage:
                    batteryState.CanDischarge = canDischargeMessage.CanDischarge;
                    break;
            }
        }

        _window?.Update(batteryState);
    }
}