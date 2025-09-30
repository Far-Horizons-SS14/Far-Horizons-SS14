using Content.Shared.FarHorizons.Factions;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles.Jobs;

public abstract partial class SharedJobSystem{
    [Dependency] private readonly ISharedFactionManager _factions = default!;

    public ProtoId<FactionPrototype>? MindGetFactionId(EntityUid? mindId) =>
        mindId is null ? null :
            _roles.MindHasRole<JobRoleComponent>(mindId.Value, out var role) ? role.Value.Comp1.FactionPrototype : null;

    public FactionPrototype? MindGetFaction(EntityUid? mindId) =>
        _prototypes.TryIndex(MindGetFactionId(mindId), out var faction) ? faction : null;

}