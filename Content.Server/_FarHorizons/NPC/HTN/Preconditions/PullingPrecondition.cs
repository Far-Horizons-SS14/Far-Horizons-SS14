using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Whitelist;

namespace Content.Server._FarHorizons.NPC.HTN.Preconditions;

/// <summary>
/// Checks whether the NPC is pulling an entity that passes an optional filter.
/// </summary>
public sealed partial class PullingPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entityManager = default!;
    private EntityWhitelistSystem _whitelist = default!;
    private PullingSystem _pulling = default!;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public bool Pulling = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pulling = sysManager.GetEntitySystem<PullingSystem>();
        _whitelist = sysManager.GetEntitySystem<EntityWhitelistSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var target = _pulling.GetPulling(owner);

        if (!Pulling)
            return target == null || _entityManager.Deleted(target.Value);

        return target != null &&
               !_entityManager.Deleted(target.Value) &&
               _whitelist.CheckBoth(target.Value, null, Whitelist);
    }
}
