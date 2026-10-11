using Content.Shared.Physics;

namespace Content.Server._FarHorizons.Shuttles;

/// <summary>
/// Component used for targeting logic, holds data that acts to determine whether gunnery turret can shoot or not.
/// </summary>
[RegisterComponent]
public sealed partial class GunneryManagedTargetingComponent : Component
{
    /// <summary>
    /// A mask used to determine whether the gun can shoot, based on what material is in front of it, if any.
    /// </summary>
    [DataField]
    public CollisionGroup TargetingCollisionMask = CollisionGroup.Impassable | CollisionGroup.BulletImpassable;
}
