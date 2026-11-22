using Robust.Shared.Serialization; 
using Content.Shared.Actions;

namespace Content.Shared._FarHorizons.Medical.Limbs;

[Serializable, NetSerializable]
public enum LimbItemToggleMenuUiKey : byte
{
    Key
}

public sealed partial class ToggleCyberlimbMenuEvent : InstantActionEvent;
[Serializable, NetSerializable]
public sealed class RefreshLimbUIMessage(NetEntity ent, bool value) : BoundUserInterfaceMessage
{
    public NetEntity Ent = ent;
    public bool Value = value;
}

[Serializable, NetSerializable]
public sealed class LimbToggleMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class LimbItemToggleMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Item;
    public LimbItemToggleMessage(NetEntity item) 
        => Item = item;
}