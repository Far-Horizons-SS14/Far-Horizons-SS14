using Content.Server.NPC;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;

namespace Content.Server._FarHorizons.NPC.Queries.Filters;

/// <summary>
/// Removes entities that the querying NPC cannot currently pull.
/// </summary>
public sealed partial class PullableFilter : ExternalFilter
{
    [DataField]
    public bool RequireUnpulled = true;

    public override List<EntityUid> GetEntities(NPCBlackboard blackboard, HashSet<EntityUid> entities, IEntityManager entMan)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var pulling = entMan.System<PullingSystem>();
        _entityList.Clear();

        foreach (var entity in entities)
        {
            if (!entMan.TryGetComponent<PullableComponent>(entity, out var pullable) ||
                RequireUnpulled && pullable.BeingPulled ||
                !pulling.CanPull(owner, entity))
            {
                _entityList.Add(entity);
            }
        }

        return _entityList;
    }
}
