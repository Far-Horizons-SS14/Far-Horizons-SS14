using Robust.Shared.GameStates;
using Robust.Shared.Containers;
using Content.Shared.Whitelist;

namespace Content.Shared._FarHorizons.Vehicles;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleContainerComponent : Component
{
    /// <summary>
    /// total seat count for the vehicle
    /// </summary>
    [DataField("seats")]
    public int Seats = 2;

    /// <summary>
    /// how long does it takes to get inside the vehicle
    /// </summary>
    [DataField("entryTime")]
    public TimeSpan EntryTime = TimeSpan.FromSeconds(1.5);
    
    /// <summary>
    /// how long does it takes to remove someone from the vehicle
    /// </summary>
    [DataField("removeTime")]
    public TimeSpan RemoveTime = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// The slot the passengers are stored in
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Container PassengerSlot = default!;

    [ViewVariables]
    public readonly string PassengerSlotId = "passenger_slot";

    /// <summary>
    /// Just a check for a if a vehicle is a air tight.
    /// </summary>
    [DataField]
    public bool isAirtight = false;

    [DataField]
    public EntityWhitelist? PassengerWhitelist;
}