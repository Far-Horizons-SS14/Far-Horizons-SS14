// Far Horizons fork of upstream Content.Client/Power/Generator/PortableGeneratorBoundUserInterface.cs, which is excluded from the build.
using Content.Shared.Access.Systems;
using Content.Shared._FarHorizons.Power.SmartGrid;
using Content.Shared.Power.Generator;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.Power.Generator;

[UsedImplicitly]
public sealed class PortableGeneratorBoundUserInterface : BoundUserInterface
{
    private GeneratorWindow? _window;

    public PortableGeneratorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindowCenteredLeft<GeneratorWindow>();
        _window.SetEntity(Owner);
        _window.OnState += args =>
        {
            if (args)
            {
                Start();
            }
            else
            {
                Stop();
            }
        };

        _window.OnPower += SetTargetPower;
        _window.OnEjectFuel += EjectFuel;
        _window.OnSwitchOutput += SwitchOutput;
        _window.OnSmartGridAvailability += SetSmartGridAvailability;
        _window.SetSmartGridAccessEnabled(LocalPlayerHasAccess());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not PortableGeneratorComponentBuiState msg)
            return;

        _window?.Update(msg);
    }

    public void SetTargetPower(int target)
    {
        SendMessage(new PortableGeneratorSetTargetPowerMessage(target));
    }

    public void Start()
    {
        SendMessage(new PortableGeneratorStartMessage());
    }

    public void Stop()
    {
        SendMessage(new PortableGeneratorStopMessage());
    }

    public void SwitchOutput()
    {
        SendMessage(new PortableGeneratorSwitchOutputMessage());
    }

    public void EjectFuel()
    {
        SendMessage(new PortableGeneratorEjectFuelMessage());
    }

    public void SetSmartGridAvailability(SmartGridPriority priority)
    {
        SendMessage(new SmartGridSetAvailabilityOverrideMessage(priority));
    }

    private bool LocalPlayerHasAccess()
    {
        return PlayerManager.LocalEntity is { } player
            && EntMan.System<AccessReaderSystem>().IsAllowed(player, Owner);
    }
}
