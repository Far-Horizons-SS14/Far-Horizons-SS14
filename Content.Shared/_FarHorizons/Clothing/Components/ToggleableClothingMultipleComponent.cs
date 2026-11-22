using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     This component gives an item an action that will equip or un-equip some clothing e.g. hardsuits and hardsuit helmets but multiple
/// </summary>
[Access(typeof(ToggleableClothingSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleableClothingMultipleComponent : Component
{
    public const string DefaultClothingContainerId = "toggleable-clothing";

    /// <summary>
    ///     Action used to open the menu to toggle the clothing on or off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionOpenMultiClothing";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    ///     Default clothing entity prototypes to spawn into the clothing container with list of strings to which slot they belong to.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<string, EntProtoId> ClothingPrototypes = new();

    /// <summary>
    ///     List of slots and which ones are active or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public Dictionary<string, bool> isActiveList = new();

    /// <summary>
    ///     The inventory slot flags required for this component to function.
    /// </summary>
    [DataField("requiredSlot"), AutoNetworkedField]
    public SlotFlags RequiredFlags = SlotFlags.OUTERCLOTHING;

    /// <summary>
    ///     The container that the clothing is stored in when not equipped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerId = DefaultClothingContainerId;

    [ViewVariables]
    public Container? Container;

    /// <summary>
    ///     The Id of the piece of clothing that belongs to this component. Required for map-saving if the clothing is
    ///     currently not inside of the container. But with multiple. String is for slot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, EntityUid?> ClothingUids = new();

    /// <summary>
    ///     Time it takes for this clothing to be toggled via the stripping menu verbs. Null prevents the verb from even showing up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? StripDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     Text shown in the toggle-clothing verb. Defaults to using the name of the <see cref="ActionEntity"/> action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, string?> VerbTexts = new()
    {
        {"head", "Toggle Head Cover"},
        {"gloves", "Toggle Gloves"},
        {"mask", "Toggle Mask"},
        {"outerClothing", "Toggle Outer Clothing"},
        {"shoes", "Toggle Shoes"},
        {"eyes", "Toggle Eye Cover"}
    };
    

    /// <summary>
    ///    
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, bool> ReplaceExistingClothing = new();
}

public sealed partial class OpenClothingToggleRadial : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class ClothingSlotToggle : BoundUserInterfaceMessage
{
    public readonly string Slot;
    public ClothingSlotToggle(string slot) 
        => Slot = slot;
}

[Serializable, NetSerializable]
public enum ClothingToggleRadialMenuUiKey : byte
{
    Key
}