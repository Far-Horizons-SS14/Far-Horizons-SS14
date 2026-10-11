using Content.Shared.Access;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Maps.FactionalAccess;

[RegisterComponent]
public sealed partial class FactionalAccessComponent : Component
{
    /// <summary>
    /// Maps an access level to the equivalent access level it should be converted to.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AccessLevelPrototype>, ProtoId<AccessLevelPrototype>> EquivalentAccessList = [];
}