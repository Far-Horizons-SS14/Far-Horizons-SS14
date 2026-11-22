using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.Generation.FusionGenerator;

#region MASER
[Serializable, NetSerializable]
public enum FusionReactorMaserVisuals
{
    Power,
    Jar,
    Injecting
}

[Serializable, NetSerializable]
public enum FusionReactorMaserVisualLayers
{
    Power,
    Jar,
    Injecting
}

[Serializable, NetSerializable]
public enum FusionReactorMaserVisualPowerLevel
{
    Level0,
    Level1,
    Level2,
    Level3,
    Level4,
    Level5
}
#endregion

#region Gas Inlet
[Serializable, NetSerializable]
public enum FusionReactorGasInletVisuals
{
    Enabled
}

[Serializable, NetSerializable]
public enum FusionReactorGasInletVisualLayers
{
    Enabled
}
#endregion

#region Coolant Pump
[Serializable, NetSerializable]
public enum FusionReactorCoolantPumpVisuals
{
    Type,
    Enabled
}

[Serializable, NetSerializable]
public enum FusionReactorCoolantPumpVisualLayers
{
    Type,
    Enabled
}

[Serializable, NetSerializable]
public enum FusionReactorCoolantPumpEnabledType
{
    Off,
    Inlet,
    Outlet
}
#endregion

#region Battery
[Serializable, NetSerializable]
public enum FusionReactorBatteryVisuals
{
    Input,
    Output,
    ChargeLevel
}

[Serializable, NetSerializable]
public enum FusionReactorBatteryVisualLayers
{
    Input,
    Output,
    ChargeLevel
}

[Serializable, NetSerializable]
public enum FusionReactorBatteryVisualChargeLevel
{
    Level0,
    Level1,
    Level2,
    Level3,
    Level4,
    Level5
}
#endregion

#region Controller
[Serializable, NetSerializable]
public enum FusionReactorControllerVisuals
{
    Display
}

[Serializable, NetSerializable]
public enum FusionReactorControllerVisualLayers
{
    Display
}

[Serializable, NetSerializable]
public enum FusionReactorControllerVisualDisplayState
{
    Off,
    Damage0,
    Damage1,
    Damage2,
    Damage3
}
#endregion

#region Torus
[Serializable, NetSerializable]
public enum FusionReactorTorusVisuals
{
    Magnet,
    Superconducting
}

[Serializable, NetSerializable]
public enum FusionReactorTorusVisualLayers
{
    Magnet,
    Superconducting
}
#endregion
