using System.Threading;
using System.Threading.Tasks;
using Content.Server.Hands.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server._FarHorizons.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SaveEntityInActiveHandOperator : HTNOperator
{
    private HandsSystem _hands = default!;

    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _hands = sysManager.GetEntitySystem<HandsSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_hands.TryGetActiveItem(owner, out var item))
            return (false, null);

        return (true, new Dictionary<string, object>()
            {
                { TargetKey, item }
            });
    }
}