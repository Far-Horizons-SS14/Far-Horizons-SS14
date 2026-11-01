using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;

namespace Content.Shared._FarHorizons.PowerArmor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerArmorUserComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid Wearing;

    /// <summary>
    /// The battery charge alert.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    /// <summary>
    /// The alert for a missing battery.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";
}
