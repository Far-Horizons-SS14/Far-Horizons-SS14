using Content.Client.UserInterface;
using Content.Shared._FarHorizons.Power.Generation.FusionGenerator;
using JetBrains.Annotations;
using Robust.Client.Timing;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.Power.UI.FusionReactor;

/// <summary>
/// Initializes a <see cref="FusionReactorMaserWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class FusionReactorMaserBoundUserInterface : BoundUserInterface, IBuiPreTickUpdate
{
    [Dependency] private readonly IClientGameTiming _gameTiming = null!;
    [Dependency] private readonly IEntityManager _entityManager = null!;

    [ViewVariables]
    private FusionReactorMaserWindow? _window;

    private BuiPredictionState? _pred;
    private InputCoalescer<int> _powerLevelCoalescer;

    public FusionReactorMaserBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) => IoCManager.InjectDependencies(this);

    protected override void Open()
    {
        base.Open();

        _pred = new BuiPredictionState(this, _gameTiming);

        _window = this.CreateWindow<FusionReactorMaserWindow>();
        _window.SetEntity(Owner);

        _window.SetPowerLevel += val => _powerLevelCoalescer.Set(val);
        _window.SetAMInject += OnInjectToggle;
        Update();
    }

    void IBuiPreTickUpdate.PreTickUpdate()
    {
        if (_powerLevelCoalescer.CheckIsModified(out var powerLevelValue))
            _pred!.SendMessage(new FusionReactorMaserSetPowerMessage(powerLevelValue));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not FusionReactorMaserBuiState maserState)
            return;

        foreach (var replayMsg in _pred!.MessagesToReplay())
        {
            switch (replayMsg)
            {
                case FusionReactorMaserSetPowerMessage setPowerLevel:
                    maserState.PowerSetting = Math.Clamp(setPowerLevel.PowerLevel, 0, maserState.MaxPowerSetting);
                    break;
            }
        }

        _window?.Update(maserState);
    }
    
    private void OnInjectToggle(bool inject)
    {
        SendPredictedMessage(new FusionReactorMaserSetInjectionMessage(inject));
    }
}