using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Body;

/// <summary>
/// Defines an organ that applies a sprite to the specified <see cref="Layer" /> within the body
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
// [Access(typeof(SharedVisualBodySystem))] // FH - I'm sorry again, but I really want to...
public sealed partial class VisualOrganComponent : Component
{
    /// <summary>
    /// The sprite layer on the entity that this contributes to
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// The sprite data for the layer
    /// </summary>
    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public PrototypeLayerData Data;

    /// <summary>
    /// When applying a profile, if the sex is present in this dictionary, overrides the state of the sprite data.
    /// Used for e.g. male vs female torsoes.
    /// </summary>
    [DataField]
    public Dictionary<Sex, string>? SexStateOverrides;

    /// <summary>
    /// For overriding the sprite path when a organ meets certain species condition.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, string>? SpeciesOverrides;

    /// <summary>
    /// The current profile data of this organ, used for alternate sprite selection and colouration.
    /// </summary>
    [DataField, AutoNetworkedField]
    public OrganProfileData Profile = new();

    [DataField] public bool ScaleSource; // Far Horizons - this organ will set scale for entire body
}

/// <summary>
/// Defines the coloration, sex, etc. of organs
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial record struct OrganProfileData
{
    /// <summary>
    /// The "sex" of this organ
    /// </summary>
    [DataField]
    public Sex Sex;

    /// <summary>
    /// The "eye color" of this organ
    /// </summary>
    [DataField]
    public Color EyeColor = Color.White;

    /// <summary>
    /// The "skin color" of this organ
    /// </summary>
    [DataField]
    public Color SkinColor = Color.White;

    // Far Horizons - added for parity with previous markings system
    [DataField] public bool EyeGlowing = false;

    [DataField] public float Width = 1;
    [DataField] public float Height = 1;
}

