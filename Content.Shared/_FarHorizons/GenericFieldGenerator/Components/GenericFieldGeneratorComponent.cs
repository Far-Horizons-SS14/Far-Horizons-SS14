using Content.Shared.Physics;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._FarHorizons.GenericFieldGenerator.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GenericFieldGeneratorComponent : Component
{
    /// <summary>
    /// How much power should this field generator consume?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("powerDrain")]
    public int PowerDrain = 100;

    /// <summary>
    /// How many tiles should this field check before giving up?
    /// </summary>
    [DataField("maxLength")]
    public float MaxLength = 8F;

    /// <summary>
    /// Is the generator toggled on?
    /// </summary>
    [DataField]
    public bool Enabled;

    /// <summary>
    /// Is the generator Charged?
    /// </summary>
    [DataField]
    public bool Charged;

    /// <summary>
    /// Is this generator connected to fields?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsConnected;

    /// <summary>
    /// The masks the raycast should not go through
    /// </summary>
    [DataField("collisionMask")]
    public int CollisionMask = (int) (CollisionGroup.MobMask | CollisionGroup.Impassable | CollisionGroup.MachineMask | CollisionGroup.Opaque);

    /// <summary>
    /// A collection of connections that the generator has based on direction.
    /// Stores a list of fields connected between generators in this direction.
    /// </summary>
    [ViewVariables]
    public Dictionary<Direction, (Entity<GenericFieldGeneratorComponent>, List<EntityUid>)> Connections = new();

    /// <summary>
    /// What fields should this spawn?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("createdField", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CreatedField = "ContainmentField";

    /// <summary>
    /// How fast should the generator charge?
    /// </summary>
    [DataField]
    public int ChargeRate = 100;
}

[Serializable, NetSerializable]
public enum GenericFieldGeneratorVisuals : byte
{
    PowerLight,
    FieldLight,
    OnLight,
}

[Serializable, NetSerializable]
public enum PowerLevelVisuals : byte
{
    NoPower,
    LowPower,
    MediumPower,
    HighPower,
}

[Serializable, NetSerializable]
public enum FieldLevelVisuals : byte
{
    NoLevel,
    On,
    OneField,
    MultipleFields,
}
