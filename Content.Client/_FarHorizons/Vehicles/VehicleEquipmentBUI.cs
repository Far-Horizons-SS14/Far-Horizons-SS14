using Content.Shared._FarHorizons.Vehicles;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.Vehicles;

public sealed class VehicleEquipmentBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private VehicleEquipmentMenu? _menu;
    public VehicleEquipmentBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {}

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<VehicleEquipmentMenu>();
        _menu.SetEntity(Owner);

        _menu.OnUninstallButtonPressed += (part, slot) =>
            SendMessage(new UninstallPartMessage(part, slot));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if(_menu == null)
            return;
    }
}