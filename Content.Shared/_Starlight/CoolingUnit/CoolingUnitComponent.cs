using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.CoolingUnit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CoolingUnitComponent : Component
{
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleCoolingUnit";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    /// <summary>
    /// Max Cooling by Sec.
    /// </summary>
    [DataField]
    public float MaxCooling = 12f;

    //FH Start

    /// <summary>
    /// For designating the desired temperature on the cooling unit.
    /// </summary>
    [DataField]
    public float? DesiredTemp;

    /// <summary>
    /// Max Temperature the cooling unit can go up to
    /// </summary>
    [DataField]
    public float MaxTemperature = 255.3f;

    /// <summary>
    /// Min Temperature the cooling unit can go down to
    /// </summary>
    [DataField]
    public float MinTemperature = 200.3f;

    /// <summary>
    /// In what slots does this cooling unit work on.
    /// </summary>
    [DataField]
    public SlotFlags RequiredSlots = SlotFlags.NONE;
}

[Serializable, NetSerializable]
public enum CoolingUnitUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum CoolingUnitVisuals : byte
{
    Enabled,
    Layer
}

[Serializable, NetSerializable]
public sealed class CoolingUnitToggleMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CoolingUnitChangeTemperatureMessage : BoundUserInterfaceMessage
{
    public float Temperature { get; }
    public CoolingUnitChangeTemperatureMessage(float temperature)
        => Temperature = temperature;
}
//FH End