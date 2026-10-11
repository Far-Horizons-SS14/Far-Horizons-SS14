using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared.Starlight;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LimbItemStorageComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Items = [];

    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, bool> ItemEntities = new(); //FH Edit

    [DataField, AutoNetworkedField]
    public string ContainerId = "cyberlimb";
}
