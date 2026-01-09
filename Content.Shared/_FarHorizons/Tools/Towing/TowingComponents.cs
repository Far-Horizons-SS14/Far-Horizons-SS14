using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.Towing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TowingComponent : Component
{
    /// <summary>
    /// how long does it take to tie up an entity
    /// </summary>
    [DataField("tieUpTime"), AutoNetworkedField]
    public TimeSpan TieUpTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// First entity tied in the rope component
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? EntityA;

    [DataField, ViewVariables]
    public SpriteSpecifier RopeSprite =
    new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");
}

[Serializable, NetSerializable]
public sealed partial class TieUpDoAfter : SimpleDoAfterEvent;
