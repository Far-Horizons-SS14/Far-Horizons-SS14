using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._FarHorizons.Shuttles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunneryConsoleComponent : Component
{
    [ViewVariables]
    public List<EntityUid> ConnectedTurrets = [];

    [DataField]
    public float CheckDistance = 16;

    [DataField("turretConnectionPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string TurretConnectionPort = "GunneryConsoleTurretControl";

    [ViewVariables, AutoNetworkedField]
    public Dictionary<NetEntity, GunneryConsoleTurretMetaData> TurretMetaData = [];
}