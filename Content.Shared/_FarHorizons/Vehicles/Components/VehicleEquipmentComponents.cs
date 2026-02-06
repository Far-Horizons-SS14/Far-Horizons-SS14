using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Vehicles.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VehicleEquipmentComponent : Component
{
    [DataField]
    public EntProtoId? ActionProto;
    
    [ViewVariables, AutoNetworkedField] 
    public EntityUid? ActionEntity;
}