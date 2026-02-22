using System.Numerics;

namespace Content.Shared._FarHorizons.Shuttles;

[RegisterComponent]
public sealed partial class TurretControlServerComponent : Component
{
    /// <summary>
    /// Turrets controlled by this turret control server
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<Entity<ControlledTurretComponent>> Turrets = [];

    /// <summary>
    /// Turrets that, for one reason or another, should be un-paired from this turret control server
    /// </summary>
    /// <remarks>
    /// This is meant for external use. If a turret control server is trying to un-pair from a turret it should not use this.
    /// </remarks>
    public List<Entity<ControlledTurretComponent>> InvalidTurrets = [];

    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2? TargetCoordinates;
}