using Content.Server.NPC;
using Content.Shared.Whitelist;

namespace Content.Server._FarHorizons.NPC.Queries.Filters;

/// <summary>
/// Removes query candidates that do not match a standard entity whitelist.
/// </summary>
public sealed partial class EntityWhitelistFilter : ExternalFilter
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    public override List<EntityUid> GetEntities(NPCBlackboard blackboard, HashSet<EntityUid> entities, IEntityManager entMan)
    {
        var whitelist = entMan.System<EntityWhitelistSystem>();
        _entityList.Clear();

        foreach (var entity in entities)
        {
            if (!whitelist.CheckBoth(entity, null, Whitelist))
                _entityList.Add(entity);
        }

        return _entityList;
    }
}
