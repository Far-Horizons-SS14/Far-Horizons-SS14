using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Content.Shared._FarHorizons.Vehicles.Equipment;

namespace Content.Client._FarHorizons.Vehicles.Equipment;

public sealed partial class VehicleEquipmentMenu : FancyWindow
{
    public VehicleEquipmentMenu()
    {
        RobustXamlLoader.Load(this);
        Log.Info("weh");
    }

    public void UpdateState(VehicleEquipmentUiState state)
    {
        Log.Info($"{state}");
        Log.Info("weh");
    }
}