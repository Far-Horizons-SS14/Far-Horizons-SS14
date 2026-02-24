using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
// [Access(typeof(BodySystem))] // FH - I know I'm being bad, but I'm very tired, just give me this
public sealed partial class OrganComponent : Component
{
    /// <summary>
    /// The body entity containing this organ, if any
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// What kind of organ is this, if any
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Category;
}
