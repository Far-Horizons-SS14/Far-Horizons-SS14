using Content.Shared._FarHorizons.Factions;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Access.Components;

[RegisterComponent]
public sealed partial class PresetIdCardComponent : Component
{
    [DataField("job")]
    public ProtoId<JobPrototype>? JobName;

    [DataField("name")]
    public string? IdName;

    // FarHorizons - custom job titles
    [DataField("customJobTitle")]
    public string? CustomJobTitle;
    
    // FarHorizons Start - Faction Support
    [DataField("faction")]
    public ProtoId<FactionPrototype>? Faction;
    // FarHorizons End
}
