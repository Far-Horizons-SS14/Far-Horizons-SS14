using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Doors.Components;


// Filler component to make querying easier
[RegisterComponent, Access(typeof(FHDoorBendSystem)), NetworkedComponent ]
public sealed partial class FHDoorComponent : Component
{
}
