using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerArmorPartComponent : Component
{
    /// <summary>
    /// The damage reduction when the part is unbroken
    /// </summary>
    [DataField(required: true)]
    public DamageModifierSet Modifiers = default!;

    /// <summary>
    /// The damage reduction when the part is broken
    /// </summary>
    [DataField(required: true)]
    public DamageModifierSet BrokenModifiers = default!;

    /// <summary>
    /// The speed penalty applied by this part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1.0f;

    /// <summary>
    /// The type of part this part is.
    /// </summary>
    [DataField(required: true)]
    public PowerArmorVisualLayers PartType = default;

    [DataField]
    public TimeSpan InstallTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The max integrity of a part.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 MaxIntegrity = FixedPoint2.Zero;

    /// <summary>
    /// Determines if the part is broken.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool isBroken = false;

    /// <summary>
    /// Is the part installed and if so to whom
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? AttachedTo;
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

[Serializable, NetSerializable]
public enum PowerArmorVisualLayers : byte
{
    UnderArmor,
    UnderArmorHelmet,
    Chest,
    Head,
    RArm,
    LArm,
    RLeg,
    LLeg
}

[Serializable, NetSerializable]
public sealed class UninstallArmorPartMessage : BoundUserInterfaceMessage
{
    public readonly PowerArmorVisualLayers PartType;
    public readonly NetEntity Part;
    public UninstallArmorPartMessage(PowerArmorVisualLayers partType, NetEntity part)
    {
        PartType = partType;
        Part = part;
    }
}

[Serializable, NetSerializable]
public sealed partial class InstallPartDoAfter : SimpleDoAfterEvent
{
    public readonly PowerArmorVisualLayers PartType;
    public readonly NetEntity Part;
    public InstallPartDoAfter(PowerArmorVisualLayers partType, NetEntity part)
    {
        PartType = partType;
        Part = part;
    }
}

[Serializable, NetSerializable]
public sealed class PowerArmorUninstallModuleMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Module;
    public PowerArmorUninstallModuleMessage(NetEntity module) 
        => Module = module;
}

[Serializable, NetSerializable]
public sealed class PowerArmorToggleModuleMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Module;
    public PowerArmorToggleModuleMessage(NetEntity module) 
        => Module = module;
}

[Serializable, NetSerializable]
public sealed class TogglePowerArmorMessage : BoundUserInterfaceMessage;

public sealed partial class TogglePowerArmorModuleActionEvent : InstantActionEvent;
public sealed partial class TogglePowerArmorMenu : InstantActionEvent;

public interface IPowerArmorRelayedEvent;

public sealed class PowerArmorRelayedEvent<TEvent> : EntityEventArgs
{
    public TEvent Args;

    public EntityUid Owner;

    public PowerArmorRelayedEvent(TEvent args, EntityUid owner)
    {
        Args = args;
        Owner = owner;
    }
}