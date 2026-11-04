using Content.Shared._Starlight.CoolingUnit;
using Content.Shared.Atmos;
using Content.Shared.Item.ItemToggle.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.CoolingUnit;
[UsedImplicitly]
public sealed class CoolingUnitBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CoolingUnitWindow? _window;

    [ViewVariables]
    private float _minTemp = 0.0f;

    [ViewVariables]
    private float _maxTemp = 0.0f;

    public CoolingUnitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey){}

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CoolingUnitWindow>();

        _window.ToggleStatusButton.OnToggled += _ => OnToggleStatusButtonPressed();
        _window.TemperatureSpinbox.OnValueChanged += _ => OnTemperatureChanged(_window.TemperatureSpinbox.Value);
        _window.Entity = Owner;
        Update();
    }

    private void OnToggleStatusButtonPressed()
        => SendPredictedMessage(new CoolingUnitToggleMessage());

    private void OnTemperatureChanged(float value)
    {
        var actual = Math.Max(value, _minTemp);
        actual = Math.Max(actual, Atmospherics.TCMB);
        if (!MathHelper.CloseTo(actual, value, 0.09))
        {
            _window?.SetTemperature(actual);
            return;
        }

        SendPredictedMessage(new CoolingUnitChangeTemperatureMessage(actual));
    }

    public override void Update()
    {
        if (_window == null 
        || !EntMan.TryGetComponent(Owner, out CoolingUnitComponent? coolingComp)
        || !EntMan.TryGetComponent(Owner, out ItemToggleComponent? toggleComp))
            return;

        _minTemp = coolingComp.MinTemperature;
        _maxTemp = coolingComp.MaxTemperature;

        _window.SetTemperature(coolingComp.DesiredTemp ?? 0f);

        // Also set in frameupdates.
        _window.SetActive(toggleComp.Activated);

        _window.Title = "";
    }
}
