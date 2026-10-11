using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.Generation.FusionGenerator;

#region MASER
[Serializable, NetSerializable]
public enum FusionReactorMaserVisuals
{
    Enabled,
    Injecting
}

[Serializable, NetSerializable]
public enum FusionReactorMaserVisualLayers
{
    Enabled,
    Injecting
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
    Enabled
}

[Serializable, NetSerializable]
public enum FusionReactorCoolantPumpVisualLayers
{
    Enabled
}

[Serializable, NetSerializable]
public enum FusionReactorCoolantPumpEnabledType
{
    InletOff,
    InletOn,
    OutletOff,
    OutletOn
}
#endregion

#region Battery
[Serializable, NetSerializable]
public enum FusionReactorBatteryVisuals
{
    ChargeLevel
}

[Serializable, NetSerializable]
public enum FusionReactorBatteryVisualLayers
{
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
    On,
    Warning,
    Critical,
    Bad
}
#endregion