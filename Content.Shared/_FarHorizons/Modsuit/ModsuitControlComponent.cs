using Content.Shared.Actions;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Modsuit;

/// <summary>
/// The main control unit for a modular suit (MODsuit).
/// This component handles suit activation, part deployment, power management, and module coordination.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedModsuitSystem))]
public sealed partial class ModsuitControlComponent : Component
{
    /// <summary>
    /// Whether the suit is currently activated (powered on).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Whether the activation is primed (warning step before sealing).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ActivationPrimed;

    /// <summary>
    /// Whether the suit is currently in the process of deploying/retracting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Activating;

    /// <summary>
    /// Whether all parts of the suit are currently deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Deployed;

    /// <summary>
    /// Maximum complexity of modules that can be installed.
    /// </summary>
    [DataField]
    public int ComplexityMax = 15;

    /// <summary>
    /// Current complexity used by installed modules.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Complexity;

    /// <summary>
    /// Slowdown applied when the suit is not active.
    /// </summary>
    [DataField]
    public float SlowdownInactive = 1.25f;

    /// <summary>
    /// Slowdown applied when the suit is active.
    /// </summary>
    [DataField]
    public float SlowdownActive = 0.75f;

    /// <summary>
    /// Time it takes to deploy/retract each part of the suit.
    /// </summary>
    [DataField]
    public TimeSpan ActivationStepTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Power drain per second when the suit is idle.
    /// </summary>
    [DataField]
    public float IdlePowerDrain = 5f;

    /// <summary>
    /// Sound played when activating the suit.
    /// </summary>
    [DataField]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/Mecha/powerup.ogg");

    /// <summary>
    /// Sound played when deactivating the suit.
    /// </summary>
    [DataField]
    public SoundSpecifier? DeactivateSound = new SoundPathSpecifier("/Audio/Mecha/powerdown.ogg");

    /// <summary>
    /// Sound played when deploying suit parts.
    /// </summary>
    [DataField]
    public SoundSpecifier? DeploySound = new SoundPathSpecifier("/Audio/Items/Guns/Pistols/gunshot.ogg");

    /// <summary>
    /// Prototype ID of the helmet that deploys with this suit.
    /// </summary>
    [DataField]
    public EntProtoId? HelmetPrototype;

    /// <summary>
    /// Prototype ID of the chestplate that deploys with this suit.
    /// </summary>
    [DataField]
    public EntProtoId? ChestplatePrototype;

    /// <summary>
    /// Prototype ID of the gauntlets that deploy with this suit.
    /// </summary>
    [DataField]
    public EntProtoId? GauntletsPrototype;

    /// <summary>
    /// Prototype ID of the boots that deploy with this suit.
    /// </summary>
    [DataField]
    public EntProtoId? BootsPrototype;

    /// <summary>
    /// Container ID for storing deployed helmet.
    /// </summary>
    [DataField]
    public string HelmetContainerId = "modsuit-helmet";

    /// <summary>
    /// Container ID for storing deployed chestplate.
    /// </summary>
    [DataField]
    public string ChestplateContainerId = "modsuit-chestplate";

    /// <summary>
    /// Container ID for storing deployed gauntlets.
    /// </summary>
    [DataField]
    public string GauntletsContainerId = "modsuit-gauntlets";

    /// <summary>
    /// Container ID for storing deployed boots.
    /// </summary>
    [DataField]
    public string BootsContainerId = "modsuit-boots";

    /// <summary>
    /// Reference to the deployed helmet entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Helmet;

    /// <summary>
    /// Reference to the deployed chestplate entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Chestplate;

    /// <summary>
    /// Reference to the deployed gauntlets entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Gauntlets;

    /// <summary>
    /// Reference to the deployed boots entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Boots;

    /// <summary>
    /// The entity wearing this modsuit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Wearer;

    /// <summary>
    /// Action for toggling suit deployment.
    /// </summary>
    [DataField]
    public EntProtoId? DeployAction = "ActionModsuitDeploy";

    /// <summary>
    /// Action for toggling suit activation.
    /// </summary>
    [DataField]
    public EntProtoId? ActivateAction = "ActionModsuitActivate";

    [DataField, AutoNetworkedField]
    public EntityUid? DeployActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? ActivateActionEntity;
}

/// <summary>
/// Component for modsuit parts (helmet, chestplate, gauntlets, boots).
/// Links the part back to its control unit.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModsuitPartComponent : Component
{
    /// <summary>
    /// The control unit this part belongs to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Control;

    /// <summary>
    /// The type of part (helmet, chestplate, gauntlets, boots).
    /// </summary>
    [DataField]
    public ModsuitPartType PartType;

    /// <summary>
    /// Whether this part is currently sealed/deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Sealed;
}

[Serializable, NetSerializable]
public enum ModsuitPartType : byte
{
    Helmet,
    Chestplate,
    Gauntlets,
    Boots
}

/// <summary>
/// Component for modsuit modules.
/// Modules add functionality to the suit.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModsuitModuleComponent : Component
{
    /// <summary>
    /// The control unit this module is installed in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Control;

    /// <summary>
    /// Module type (passive, toggle, usable, active).
    /// </summary>
    [DataField]
    public ModsuitModuleType ModuleType = ModsuitModuleType.Passive;

    /// <summary>
    /// Whether the module is currently active/toggled on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// How much complexity this module uses.
    /// </summary>
    [DataField]
    public int Complexity = 1;

    /// <summary>
    /// Power drain when idle.
    /// </summary>
    [DataField]
    public float IdlePowerCost = 0f;

    /// <summary>
    /// Power drain when active.
    /// </summary>
    [DataField]
    public float ActivePowerCost = 0f;

    /// <summary>
    /// Power cost per use.
    /// </summary>
    [DataField]
    public float UsePowerCost = 0f;

    /// <summary>
    /// Whether this module can be removed.
    /// </summary>
    [DataField]
    public bool Removable = true;
}

[Serializable, NetSerializable]
public enum ModsuitModuleType : byte
{
    /// <summary>
    /// Always active, no interaction needed.
    /// </summary>
    Passive,

    /// <summary>
    /// Can be toggled on/off.
    /// </summary>
    Toggle,

    /// <summary>
    /// Single-use activation.
    /// </summary>
    Usable,

    /// <summary>
    /// Active module that can be selected and used.
    /// </summary>
    Active
}
