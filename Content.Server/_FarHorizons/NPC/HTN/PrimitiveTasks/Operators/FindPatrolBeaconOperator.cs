using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared._FarHorizons.NPC;
using Content.Shared.Pinpointer;
using Robust.Shared.Map;

namespace Content.Server._FarHorizons.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Finds a reachable enabled nav-map beacon matching the bot's patrol label.
/// </summary>
public sealed partial class FindPatrolBeaconOperator : HTNOperator
{
    [Dependency] private IEntityManager _entityManager = default!;
    private PathfindingSystem _pathfinding = default!;

    [DataField("targetCoordinates")]
    public string TargetCoordinates = "TargetCoordinates";

    [DataField("pathfindKey")]
    public string PathfindKey = NPCBlackboard.PathfindKey;

    [DataField("targetBeaconKey")]
    public string TargetBeaconKey = "PatrolBeaconTarget";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entityManager.TryGetComponent(owner, out SimpleBotConfigurationComponent? configuration) ||
            configuration.PatrolMode == SimpleBotPatrolMode.Wander ||
            string.IsNullOrWhiteSpace(configuration.PatrolBeacon) ||
            !_entityManager.TryGetComponent(owner, out TransformComponent? ownerTransform) ||
            ownerTransform.GridUid is not { } grid)
        {
            return (false, null);
        }

        PathResultEvent? bestPath = null;
        EntityCoordinates bestCoordinates = default;
        EntityUid bestBeacon = EntityUid.Invalid;

        foreach (var (beacon, beaconTransform) in _entityManager.EntityQuery<NavMapBeaconComponent, TransformComponent>(true))
        {
            if (!beacon.Enabled ||
                beacon.Text != configuration.PatrolBeacon ||
                beaconTransform.GridUid != grid)
            {
                continue;
            }

            var path = await _pathfinding.GetPath(
                owner,
                ownerTransform.Coordinates,
                beaconTransform.Coordinates,
                0.5f,
                cancelToken,
                _pathfinding.GetFlags(blackboard));

            if (path.Result != PathResult.Path ||
                bestPath != null && path.Path.Count >= bestPath.Path.Count)
            {
                continue;
            }

            bestPath = path;
            bestCoordinates = beaconTransform.Coordinates;
            bestBeacon = beacon.Owner;
        }

        if (bestPath == null)
            return (false, null);

        return (true, new Dictionary<string, object>
        {
            { TargetCoordinates, bestCoordinates },
            { PathfindKey, bestPath },
            { TargetBeaconKey, bestBeacon },
        });
    }
}
