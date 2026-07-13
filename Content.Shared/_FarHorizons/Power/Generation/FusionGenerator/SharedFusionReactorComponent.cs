using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.Generation.FusionGenerator;

[Serializable, NetSerializable]
public enum FusionReactorUiKey : byte
{
    Key,
}

#region MASER
[Serializable, NetSerializable]
public sealed class FusionReactorMaserBuiState : BoundUserInterfaceState
{
    public int PowerSetting;
    public int MaxPowerSetting;
    public bool AMInjection;

    public NetEntity? AMJar;
}

[Serializable, NetSerializable]
public sealed class FusionReactorMaserSetPowerMessage(int power) : BoundUserInterfaceMessage
{
    public int PowerLevel { get; } = power;
}

[Serializable, NetSerializable]
public sealed class FusionReactorMaserSetInjectionMessage(bool inject) : BoundUserInterfaceMessage
{
    public bool InjectAM { get; } = inject;
}
#endregion

#region Gas Inlet
[Serializable, NetSerializable]
public sealed class FusionReactorGasInletBuiState : BoundUserInterfaceState
{
    public float PowerSetting;
    public float MaxPowerSetting;
    public bool Enabled;
}

[Serializable, NetSerializable]
public sealed class FusionReactorGasInletSetPowerMessage(float power) : BoundUserInterfaceMessage
{
    public float PowerLevel { get; } = power;
}

[Serializable, NetSerializable]
public sealed class FusionReactorGasInletSetEnableMessage(bool enable) : BoundUserInterfaceMessage
{
    public bool Enable { get; } = enable;
}
#endregion

#region Coolant Pump
[Serializable, NetSerializable]
public sealed class FusionReactorCoolantPumpSetFlowMessage(float flowRate) : BoundUserInterfaceMessage
{
    public float FlowRate { get; } = flowRate;
}

[Serializable, NetSerializable]
public sealed class FusionReactorCoolantPumpSetEnableMessage(bool enable) : BoundUserInterfaceMessage
{
    public bool Enable { get; } = enable;
}
#endregion