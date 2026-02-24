using Robust.Client.UserInterface;
using Content.Shared._FarHorizons.Vehicles.Equipment;

namespace Content.Client._FarHorizons.Vehicles.Equipment;

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
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if(_menu == null)
            return;
        
        switch(state)
        {
            case VehicleEquipmentUiState msg:
                _menu.UpdateState(msg);
                break;
        }
    }
}