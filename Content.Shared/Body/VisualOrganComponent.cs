using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Body;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
// [Access(typeof(SharedVisualBodySystem))] // FH - I'm sorry again, but I really want to...
public sealed partial class VisualOrganComponent : Component
{
    /// <summary>
    /// The layer on the entity that this contributes to
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// The data for the layer
    /// </summary>
    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public PrototypeLayerData Data;

    /// <summary>
    /// When applying a profile, if the sex is present in this dictionary, overrides the state of the data.
    /// </summary>
    [DataField]
    public Dictionary<Sex, string>? SexStateOverrides;

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

