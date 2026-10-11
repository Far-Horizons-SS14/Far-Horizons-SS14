using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;

namespace Content.Server._FarHorizons.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class GetEntityCoordinatesOperator : HTNOperator
{
    [Dependency] private IEntityManager _entMan = default!;

    [DataField(required: true)] public string EntityKey;
    [DataField(required: true)] public string TargetKey;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var source = blackboard.GetValue<EntityUid>(EntityKey);

        if (!_entMan.TryGetComponent<TransformComponent>(source, out var sourceTransform))
            return (false, null);

        return (true, new Dictionary<string, object>()
            {
                { TargetKey, new EntityCoordinates(sourceTransform.GridUid ?? sourceTransform.MapUid ?? source, sourceTransform.LocalPosition) }
            });
    }
}