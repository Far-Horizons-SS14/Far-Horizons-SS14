using Content.Server.NPC;
using Content.Server.NPC.Queries.Considerations;

namespace Content.Server._FarHorizons.NPC.Queries.Considerations;

// The wizden code has a giant ass switch statement that declares code for every single UtilityConsideration in one function. I'd rather not do that and move all AI code we add away from one giant function and into something generic
public abstract partial class ExternalConsideration : UtilityConsideration
{
    public virtual float GetScore(NPCBlackboard blackboard, EntityUid targetUid, IEntityManager entMan) => throw new NotImplementedException();
}