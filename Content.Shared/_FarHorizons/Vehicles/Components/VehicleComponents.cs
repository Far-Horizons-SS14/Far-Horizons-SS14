using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Content.Shared.Whitelist;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Content.Shared.Actions;

namespace Content.Shared._FarHorizons.Vehicles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VehicleComponent : Component
{
    /// <summary>
    /// The person in control of this vehicle
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Rider;

    /// <summary>
    /// The list of passengers in the vehicle
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> Passengers = new();

    /// <summary>
    /// check if a vehicle requires ignition before allowing it to move
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequireIgnition = false;

    /// <summary>
    /// Check for keys in the vehicle
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool hasKeys = false;

    /// <summary>
    /// check if a vehicle requires ignition before allowing it to move
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Started = false;

    /// <summary>
    /// Is it powered by a power cell?
    /// </summary>
    [DataField]
    public bool CellPowered = true;

    /// <summary>
    /// check if a person is allow to wield a weapon for two handed bonuses
    /// </summary>
    [DataField("disallowWielding")]
    public bool DisallowWieldingGuns = false;

    /// <summary>
    /// check if a person takes stamina damage from shooting while in a vehicle
    /// </summary>
    [DataField("allowGunKnockback")]
    public bool AllowGunKnockback = false;

    /// <summary>
    /// just to check for if the vehicle is moving for other things
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool isMoving = false;

    /// <summary>
    /// just to check for if the vehicle is broken
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool isBroken = false;

    /// <summary>
    /// How many hands are blocked by the vehicle
    /// </summary>
    [DataField("handsNeeded")]
    public int HandsNeeded = 2;

    /// <summary>
    /// Vehicle Integrity
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 MaxIntegrity = 0;

    /// <summary>
    /// how long does it take the vehicle to start
    /// </summary>
    [DataField("startupTime"), AutoNetworkedField]
    public TimeSpan startupTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// how long does it take the keys from a vehicle
    /// </summary>
    [DataField("timeToStealKeys"), AutoNetworkedField]
    public TimeSpan timeToStealKeys = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Trigger crash?
    /// </summary>
    [DataField("allowCrashing"), AutoNetworkedField]
    public bool AllowCrashing = false;

    /// <summary>
    /// Basically what portion of the damage done to the vehicle is transferred to the passengers
    /// take into account this multiplier will also be divided across all the passengers so 20% damage will be 5% to each passenger if there is 4 passengers
    /// </summary>
    [DataField("damageTransfer")]
    public float DamageTransferMultiplier = 1.0f;

    /// <summary>
    /// Sound played whenever the vehicle is started
    /// </summary>
    [DataField]
    public SoundSpecifier? StartUp;

    /// <summary>
    /// Sound played whenever the horn is press HONK
    /// </summary>
    [DataField]
    public SoundSpecifier? HornSound;

    /// <summary>
    /// Sound played whenever running over someone or crashing
    /// </summary>
    [DataField("soundHit", required: true)]
    public SoundSpecifier SoundHit = default!;

    [DataField]
    public EntityWhitelist? RiderWhitelist;

    [DataField]
    public EntityWhitelist? RiderBlacklist;

    [DataField]
    public string? BaseState;

    [DataField]
    public string? BrokenState;
}

#region Events

[ByRefEvent]
public readonly record struct AddRiderActions(EntityUid Rider);

[ByRefEvent]
public readonly record struct RemoveRiderActions(EntityUid Rider);

[ByRefEvent]
public readonly record struct TurnOffVehicleEvent();

[Serializable, NetSerializable]
public sealed partial class VehicleRemoveDoAfter : SimpleDoAfterEvent
{
    public readonly NetEntity Passenger;
    public VehicleRemoveDoAfter(NetEntity passenger) 
        => Passenger = passenger;
}

[Serializable, NetSerializable]
public sealed partial class VehicleEntryDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class VehicleUnbuckleDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class TurnKeysDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class EjectKeysDoAfter : SimpleDoAfterEvent;

public sealed partial class TurnKeysEvent : InstantActionEvent;

public sealed partial class HornActionEvent : InstantActionEvent;

public sealed partial class ToggleTrunkActionEvent : InstantActionEvent;

#endregion