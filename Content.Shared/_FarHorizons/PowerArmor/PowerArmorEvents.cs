using Robust.Shared.Serialization;
using Content.Shared.Actions;
using Content.Shared.DoAfter;

namespace Content.Shared._FarHorizons.PowerArmor;

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

[ByRefEvent]
public sealed class PowerArmorSlowdownQueryEvent : EntityEventArgs, IPowerArmorRelayedEvent
{
    /// <summary>
    /// The Total of all slowdowns
    /// </summary>
    public float TotalSlowdown = 1.0f;
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
