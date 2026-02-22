using System.Numerics;

namespace Content.Shared._FarHorizons.Shuttles;

[RegisterComponent]
public sealed partial class ControlledTurretComponent : Component
{
    [DataField]
    public Angle AngleTolerance = Angle.FromDegrees(5.0);

    /// <summary>
    /// The angle the turret was facing when it was first placed
    /// </summary>
    [ViewVariables]
    public Angle LocalRot = Angle.Zero;

    /// <summary>
    /// Position the turret should aim towards
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2? TargetCoordinates;

    /// <summary>
    /// Rotation the turret is attempting to face
    /// </summary>
    [ViewVariables]
    public Angle? TargetRotation;

    [DataField]
    public Angle AngleBounds = Angle.FromDegrees(30.0);

    [DataField]
    public double RotationSpeed = 60f;

    /// <summary>
    /// Flag indicating the turret is capable of facing the target
    /// </summary>
    [ViewVariables]
    public bool TargetInBounds = false;

    /// <summary>
    /// The turret control server this turret belongs to, if any
    /// </summary>
    [ViewVariables]
    public Entity<TurretControlServerComponent>? ControlServer;

    [ViewVariables(VVAccess.ReadWrite)]
    public ControlledTurretFiringState State = ControlledTurretFiringState.Idle;
}

public enum ControlledTurretFiringState
{
    Idle, // Not doing anything in particular
    Moving, // Rotating to face a target
    Ready, // Aimed at a target, ready to fire
    Firing,
}