using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
    /// check if a vehicle requires ignition before allowing it to move
    /// </summary>
    [DataField("requireIgnition")]
    public bool requireIgnition = false;

    /// <summary>
    /// check if a vehicle requires ignition before allowing it to move
    /// </summary>
    [DataField]
    public bool Started = false;

    /// <summary>
    /// how long does it take the vehicle to start
    /// </summary>
    [DataField("startupTime")]
    public TimeSpan startupTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// how long does it take the keys from a vehicle
    /// </summary>
    [DataField("timeToStealKeys")]
    public TimeSpan timeToStealKeys = TimeSpan.FromSeconds(3);

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

    #region Action Prototypes
    [DataField]
    public EntProtoId TurnKeysAction = "ActionTurnKeys";
    #endregion
    [DataField] public EntityUid? TurnKeysActionEntity;

}