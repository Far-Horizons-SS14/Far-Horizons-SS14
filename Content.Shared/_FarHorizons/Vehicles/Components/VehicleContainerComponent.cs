using Robust.Shared.GameStates;
using Robust.Shared.Containers;

namespace Content.Shared._FarHorizons.VehicleContainer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VehicleContainerComponent : Component
{
    /// <summary>
    /// total seat count for the vehicle
    /// </summary>
    [DataField("seats"), AutoNetworkedField]
    public int Seats = 2;

    /// <summary>
    /// how long does it takes to get inside the vehicle
    /// </summary>
    [DataField("entryTime"), AutoNetworkedField]
    public TimeSpan EntryTime = TimeSpan.FromSeconds(1.5);
    
    /// <summary>
    /// how long does it takes to remove someone from the vehicle
    /// </summary>
    [DataField("removeTime"), AutoNetworkedField]
    public TimeSpan RemoveTime = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// The uid of all the passengers
    /// </summary>
    [DataField]
    public List<EntityUid> Passengers = new();

    /// <summary>
    /// The slot the passengers are stored in
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Container PassengerSlot = default!;

    [ViewVariables]
    public readonly string PassengerSlotId = "passenger_slot";
}