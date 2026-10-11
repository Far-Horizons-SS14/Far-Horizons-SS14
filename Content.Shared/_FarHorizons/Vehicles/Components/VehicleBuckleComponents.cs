using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Vehicles;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleBuckleComponent : Component
{
    /// <summary>
    /// How long it will take to unbuckle a driver
    /// </summary>
    [DataField("unbuckleTime")]
    public TimeSpan duration = TimeSpan.FromSeconds(3f);

    /// <summary>
    /// Should stuns dismount the driver? 
    /// </summary>
    [DataField("dismountOnStun")]
    public bool stundismount = true;

    /// <summary>
    /// Should knockdowns dismount the driver?
    /// </summary>
    [DataField("dismountOnKnockdown")]
    public bool knockdowndismount = true;

    /// <summary>
    /// Should armor slow down vehicle
    /// </summary>
    [DataField("armorAffectsVehicle")]
    public bool armoraffectsvehicle = false;
    
    /// <summary>
    /// Should crashes eject driver
    /// </summary>
    [DataField("ejectOnCrash")]
    public bool EjectOnCrash = false;
}