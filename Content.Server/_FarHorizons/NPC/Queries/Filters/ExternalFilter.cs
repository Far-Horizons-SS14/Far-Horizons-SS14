using Content.Server.NPC;
using Content.Server.NPC.Queries.Queries;

namespace Content.Server._FarHorizons.NPC.Queries.Filters;

// The wizden code has a giant ass switch statement that declares code for every single UtilityConsideration in one function. I'd rather not do that and move all AI code we add away from one giant function and into something generic
public abstract partial class ExternalFilter : UtilityQueryFilter
{
    public List<EntityUid> _entityList = new();
    public virtual List<EntityUid> GetEntities(NPCBlackboard blackboard, HashSet<EntityUid> entities, IEntityManager entMan) 
        => throw new NotImplementedException();
}