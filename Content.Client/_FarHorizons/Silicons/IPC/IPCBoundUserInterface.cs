using Content.Shared._FarHorizons.Silicons.IPC;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.Silicons.IPC;

public sealed partial class IPCBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private IPCMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<IPCMenu>();
        _menu.SetEntity(Owner);

        _menu.BrainButtonPressed += () =>
        {
            SendMessage(new IPCEjectBrainBuiMessage());
        };

        _menu.EjectBatteryButtonPressed += () =>
        {
            SendMessage(new IPCEjectBatteryBuiMessage());
        };

        _menu.NameChanged += name =>
        {
            SendMessage(new IPCSetNameBuiMessage(name));
        };
    }
}