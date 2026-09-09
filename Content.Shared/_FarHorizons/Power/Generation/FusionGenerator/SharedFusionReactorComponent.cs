using Content.Shared._FarHorizons.Fusion;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.Generation.FusionGenerator;

/// This file is just a pile of all the different BUI states and messages for the fusion reactor, 
/// along with a few data types that had to be in shared rather than server only

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

    public float RequestedPower;
    public float ReceivedPower;

    public NetEntity? AMJar;
    public int Antimatter;
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

#region Batteries
[Serializable, NetSerializable]
public sealed class FusionReactorBatteryBuiState : BoundUserInterfaceState
{
    public float Charge;
    public float MaxCharge;
    public float ExternalInput;
    public float MaxExternalInput;
    public float MinMaxExternalInput;
    public float MaxMaxExternalInput;
    public float Efficiency;
    public float Input;
    public float Output;
    public bool CanCharge;
    public bool CanDischarge;
}

[Serializable, NetSerializable]
public sealed class FusionReactorBatterySetMaxInputMessage(float max) : BoundUserInterfaceMessage
{
    public float MaxExternalInput { get; } = max;
}

[Serializable, NetSerializable]
public sealed class FusionReactorBatterySetCanChargeMessage(bool canCharge) : BoundUserInterfaceMessage
{
    public bool CanCharge { get; } = canCharge;
}

[Serializable, NetSerializable]
public sealed class FusionReactorBatterySetCanDischargeMessage(bool canDischarge) : BoundUserInterfaceMessage
{
    public bool CanDischarge { get; } = canDischarge;
}
#endregion

#region Controller
[Serializable, NetSerializable]
public sealed class FusionReactorControllerBuiState : BoundUserInterfaceState
{
    /// Reactor statistics
    public FusionMixEntry Plasma;
    public Dictionary<FusionAtom, double> Stored = [];
    public float RequestedMagneticPressure;
    public Dictionary<NetEntity, FusionReactorMaserBuiState> Masers = [];
    public List<(NetEntity, FusionReactorBatteryBuiState)> Batteries = [];
    public float MagnetTemperature;
    public float MagnetCritical;
    public float Integrity;
    public float Stability;

    public FusionReactorMeltdownStage MeltdownStage;
    public bool CanEject;
    public TimeSpan EventTime;

    /// Controller data
    public bool IsMaster = false;
    public FusionReactorPowerExtractType ExtractMode;
    public float WattSetting;
    public float TempSetting;
    public Dictionary<FusionAtom, FusionReactorTransferData> Transfers = [];
    public float PowerExtracted;
    public float PowerExported;
}

[Serializable, NetSerializable]
public sealed class FusionReactorControllerSetInjectMessage(FusionAtom atom, FusionReactorTransferData data) : BoundUserInterfaceMessage
{
    public FusionAtom Atom { get; } = atom;
    public FusionReactorTransferData Data { get; } = data;
}

[Serializable, NetSerializable]
public sealed class FusionReactorControllerSetExtractMessage(FusionReactorPowerExtractType mode, float temperature, float watts) : BoundUserInterfaceMessage
{
    public FusionReactorPowerExtractType Mode { get; } = mode;
    public float TempSetting { get; } = temperature;
    public float WattSetting { get; } = watts;
}

[Serializable, NetSerializable]
public sealed class FusionReactorControllerSetPressureMessage(float pressure) : BoundUserInterfaceMessage
{
    public float RequestedMagneticPressure { get; } = pressure;
}

[Serializable, NetSerializable]
public sealed class FusionReactorControllerSetMaserPowerMessage(NetEntity maser, FusionReactorMaserSetPowerMessage message) : BoundUserInterfaceMessage
{
    public NetEntity Maser { get; } = maser;
    public FusionReactorMaserSetPowerMessage Message { get; } = message;
}

[Serializable, NetSerializable]
public sealed class FusionReactorControllerSetMaserInjectMessage(NetEntity maser, FusionReactorMaserSetInjectionMessage message) : BoundUserInterfaceMessage
{
    public NetEntity Maser { get; } = maser;
    public FusionReactorMaserSetInjectionMessage Message { get; } = message;
}

[Serializable, NetSerializable]
public sealed class FusionReactorControllerEditInjectMessage(FusionAtom atom) : BoundUserInterfaceMessage
{
    public FusionAtom Atom { get; } = atom;
}

[Serializable, NetSerializable]
public sealed class FusionReactorControllerEjectMessage() : BoundUserInterfaceMessage;
#endregion

#region Data Types
/// <summary>
/// A stripped down and net serializable version of <see cref="FusionMixture"/>, for use in UIs
/// </summary>
[Serializable, NetSerializable]
public readonly struct FusionMixEntry(FusionMixture mixture)
{
    public readonly Dictionary<FusionAtom, double> Atoms = mixture.Atoms;

    /// The Client doesn't need double precision for the rest, so it can have floats
    public readonly float Pressure = (float)mixture.Pressure;
    public readonly float Volume = (float)mixture.Volume;
    public readonly float ConstrainedPressure = (float)mixture.ConstrainedPressure;
    public readonly float ConstrainedVolume = (float)mixture.ConstrainedVolume;
    public readonly float Temperature = (float)mixture.Temperature;
    public readonly float TotalMoles = (float)mixture.TotalMoles;
}

[DataDefinition, Serializable, NetSerializable]
public partial struct FusionReactorTransferData
{
    public FusionReactorTransferType transferType = FusionReactorTransferType.SetRate;
    public float Quantity = 0;

    public FusionReactorTransferData(FusionReactorTransferType type) : this() => transferType = type;

    public FusionReactorTransferData(FusionReactorTransferType type, float quantity) : this()
    {
        transferType = type;
        Quantity = quantity;
    }
}

public enum FusionReactorPowerExtractType
{
    Disabled,
    Watts,
    Temperature,
}

public enum FusionReactorTransferType
{
    /// <summary>
    /// Add/remove atoms at a set rate
    /// </summary>
    SetRate,

    /// <summary>
    /// Keep atoms at a set level
    /// </summary>
    SetLevel,

    /// <summary>
    /// Move all into the plasma
    /// </summary>
    Fill,

    /// <summary>
    /// Remove all from the plasma
    /// </summary>
    Drain,
}

[Flags]
public enum FusionReactorMeltdownStage
{
    /// <summary>
    /// Running normally, at 100% integrity
    /// </summary>
    Stage0 = 1 << 0,

    /// <summary>
    /// Taking damage, but above 25% integrity
    /// </summary>
    /// <remarks>
    /// Will announce integrity at intervals over radio <para/>  
    /// Resolved by just stopping damage
    /// </remarks>
    Stage1 = 1 << 1,

    /// <summary>
    /// Critically damaged
    /// </summary>
    /// <remarks>
    /// Will announce integrity at intervals over radio <para/>
    /// Will make a station-wide announcement <para/>
    /// At 5% integrity will attempt to contain the plasma by dumping coolant
    /// </remarks>
    Stage2 = 1 << 2,

    /// <summary>
    /// Melting down, at or below 0% integrity
    /// </summary>
    /// <remarks>
    /// Will make a station-wide announcement <para/>
    /// Will not explode for another 15 seconds after entering <para/>
    /// Core eject option enabled
    /// </remarks>
    Stage3 = 1 << 3,

    /// <summary>
    /// Actively exploding
    /// </summary>
    /// <remarks>
    /// AllowMassDestruction only <para/>
    /// After 30 seconds will explode with a good portion of the power of the nuclear bomb
    /// </remarks>
    Stage4 = 1 << 4,

    /// <summary>
    /// Stages that are considered relatively "safe"
    /// </summary>
    /// <remarks>
    /// Will make a station-wide announcement if the reactor was in an unsafe state before
    /// </remarks>
    SafeStages = Stage0 | Stage1
}
#endregion