using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Execution;

[Serializable, NetSerializable]
public sealed partial class MeleeExecutionDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class GunExecutionDoAfterEvent : SimpleDoAfterEvent
{
}
