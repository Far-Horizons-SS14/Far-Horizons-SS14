using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.PowerArmor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerArmorComponent : Component
{
    /// <summary>
    /// The total speed penalty applied by this power armor.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float TotalSpeedModifier = 1.0f;

    /// <summary>
    /// Basically the helmet or the suit depending on which one you are looking at.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid OtherHalf;

    /// <summary>
    /// Basically is this the suit or not.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsPrimary = true;

    /// <summary>
    /// Checks if the suit is powered.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsPowered = false;

    /// <summary>
    /// Who is wearing the suit
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Wearer;

    /// <summary>
    /// Which part is the player aiming to uninstall
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? UninstallTarget;

    /// <summary>
    /// List of all currently installed modules.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> Modules = new();

    /// <summary>
    /// All the parts assigned to this power armor
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<PowerArmorVisualLayers, EntityUid?> Parts = new();

    /// <summary>
    /// Max Modding capacity for a suit.
    /// </summary>
    [DataField]
    public int MaxComplexity = 20;

    /// <summary>
    /// Movement speed when suit is powered
    /// </summary>
    [DataField]
    public float PoweredMovementSpeedModifier = 0.6f;

    /// <summary>
    /// Movement speed when suit is unpowered
    /// </summary>
    [DataField]
    public float UnpoweredMovementSpeedModifier = 0.4f;
}

[Serializable, NetSerializable]
public enum PowerArmorMenuUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum PowerArmorRadialMenuUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum PowerArmorPartVisuals : byte
{
    PowerArmor,
    Visible
}