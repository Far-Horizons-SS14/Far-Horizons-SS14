using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Vehicles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleComponent : Component
{
    /// <summary>
    /// The person in control of this vehicle
    /// </summary>
    [DataField("rider")]
    public EntityUid? Rider;

    /// <summary>
    /// check if a vehicle requires keys before allowing it to move
    /// </summary>
    [DataField("requireKeys")]
    public bool requireKeys = false;

    /// <summary>
    /// the levels of friction the wearer is subected to, higher the number the more friction.
    /// </summary>
    [DataField]
    public float Friction = 2;

    /// <summary>
    /// Determines the turning ability of the wearer, Higher the number the less control of their turning ability.
    /// </summary>
    [DataField]
    public float FrictionNoInput = 6;

    /// <summary>
    /// Sets the speed in which the wearer accelerates to full speed, higher the number the quicker the acceleration.
    /// </summary>
    [DataField]
    public float Acceleration = 2;
    
    [DataField]
    public string? BaseState;

    [DataField("autoAnimate")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AutoAnimate = true;
}