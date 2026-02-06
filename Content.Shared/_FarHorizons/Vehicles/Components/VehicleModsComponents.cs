using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;

namespace Content.Shared._FarHorizons.Vehicles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleModsComponent : Component
{
    [ViewVariables]
    public readonly string ModContainer = "vehicle_mods_container";

    [ViewVariables(VVAccess.ReadWrite)]
    public Container ModSlot = default!;

    [DataField]
    public HashSet<EntProtoId> StartingEquipment = new();

    [ViewVariables]
    public HashSet<EntityUid> SpawnedEquipment = new();

    [ViewVariables]
    public EntityUid? GasTank;
}