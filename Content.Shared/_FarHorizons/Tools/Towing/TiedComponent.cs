using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared._FarHorizons.Towing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TiedComponent : Component
{
    /// <summary>
    /// how long does it take to untie an entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UntieTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Attached to entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AttachedTo;
}

[Serializable, NetSerializable]
public sealed partial class UnTieDoAfter : SimpleDoAfterEvent;