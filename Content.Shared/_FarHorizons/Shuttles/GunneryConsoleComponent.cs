using System.Numerics;
using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._FarHorizons.Shuttles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class GunneryConsoleComponent : Component
{
    /// <summary>
    /// Turrets connected to this gunnery console
    /// </summary>
    /// <remarks>Server-side only</remarks>
    [ViewVariables]
    public List<EntityUid> ConnectedTurrets = [];

    /// <summary>
    /// Turrets currently selected in the UI
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<NetEntity> SelectedTurrets = [];

    /// <summary>
    /// Turrets that have been visibly moved by this gunnery console
    /// </summary>
    /// <remarks>Client-side only</remarks>
    [ViewVariables]
    public Dictionary<EntityUid, Vector2?> MovingTurrets = [];

    /// <summary>
    /// Currently targeted position
    /// </summary>
    /// <remarks>Updated by both client and server. Client for responsiveness, server for consistency.</remarks>
    [ViewVariables, AutoNetworkedField]
    public Vector2? TargetPosition;

    /// <summary>
    /// How quickly connected turrets can visually rotate in deg/s
    /// </summary>
    [DataField]
    public float MoveSpeed = 90;

    /// <summary>
    /// How far the turrets should check for self collision
    /// </summary>
    [DataField]
    public float CheckDistance = 16;

    [DataField("turretConnectionPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string TurretConnectionPort = "GunneryConsoleTurretControl";

    [ViewVariables, AutoNetworkedField]
    public Dictionary<NetEntity, GunneryConsoleTurretMetaData> TurretMetaData = [];
}