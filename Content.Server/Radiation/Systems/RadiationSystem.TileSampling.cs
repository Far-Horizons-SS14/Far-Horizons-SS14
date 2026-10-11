using System.Buffers;
using System.Numerics;
using Content.Server.Radiation.Components;
using Content.Shared.Radiation.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Threading;
using Robust.Shared.Timing;

namespace Content.Server.Radiation.Systems;

// This file was added by Far Horizons as a place for Funky-Station-originating
// code for grid-based radiation queries, after we moved it out of the Wizden-
// originating code in RadiationSystem after a refactor on the Wizden side.
// As part of the move, this code was parallelized in a similar fashion to the 

public partial class RadiationSystem
{
    [Dependency] private IGameTiming _gameTiming = default!;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private readonly Dictionary<EntityUid, Dictionary<Vector2i, (float Radiation, TimeSpan ExpiresAt)>>
        _tileRadiationCache = new();

    private void InitializeTileSampling()
    {
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    /// <summary>
    ///     Gets the approximate cached radiation level at a tile.
    /// </summary>
    /// <returns>Zero when the tile is not currently sampled.</returns>
    public float GetRadiationAtCoordinates(EntityCoordinates coordinates)
    {
        if (!_gridQuery.TryGetComponent(coordinates.EntityId, out var grid))
            return 0f;

        var tilePos = _maps.TileIndicesFor(coordinates.EntityId, grid, coordinates);
        return _tileRadiationCache.TryGetValue(coordinates.EntityId, out var tileCache) &&
               tileCache.TryGetValue(tilePos, out var data)
            ? data.Radiation
            : 0f;
    }

    /// <summary>
    ///     Requests a tile radiation sample during the next radiation update.
    /// </summary>
    /// <param name="coordinates">The tile coordinates to sample.</param>
    /// <param name="timeoutSeconds">How long the sample remains active without renewal.</param>
    public void RequestTileRadiationSampling(EntityCoordinates coordinates, float timeoutSeconds = 5f)
    {
        var gridUid = coordinates.GetGridUid(EntityManager);
        if (gridUid is not { } uid || !_gridQuery.TryGetComponent(uid, out var grid))
            return;

        var tilePos = _maps.TileIndicesFor((uid, grid), coordinates);
        var expires = _gameTiming.CurTime + TimeSpan.FromSeconds(timeoutSeconds);

        if (!_tileRadiationCache.TryGetValue(uid, out var samples))
        {
            samples = new Dictionary<Vector2i, (float, TimeSpan)>();
            _tileRadiationCache[uid] = samples;
        }

        samples[tilePos] = samples.TryGetValue(tilePos, out var existing)
            ? (existing.Radiation, expires)
            : (0f, expires);
    }

    private void UpdateTileRadiationCache()
    {
        var now = _gameTiming.CurTime;
        var workItems = new List<TileSamplingWorkItem>();
        var gridsToRemove = new List<EntityUid>();

        foreach (var (gridUid, samples) in _tileRadiationCache)
        {
            if (!_gridQuery.TryGetComponent(gridUid, out var grid) ||
                !_xformQuery.TryGetComponent(gridUid, out var gridTransform))
            {
                gridsToRemove.Add(gridUid);
                continue;
            }

            var samplesToRemove = new List<Vector2i>();
            foreach (var (tilePos, (_, expiresAt)) in samples)
            {
                if (now > expiresAt)
                {
                    samplesToRemove.Add(tilePos);
                    continue;
                }

                var worldPosition = _maps.GridTileToWorld(gridUid, grid, tilePos).Position;
                workItems.Add(new TileSamplingWorkItem(gridUid, tilePos, gridTransform, worldPosition));
            }

            foreach (var tilePos in samplesToRemove)
            {
                samples.Remove(tilePos);
            }

            if (samples.Count == 0)
                gridsToRemove.Add(gridUid);
        }

        foreach (var gridUid in gridsToRemove)
        {
            _tileRadiationCache.Remove(gridUid);
        }

        if (workItems.Count == 0)
            return;

        var results = new float[workItems.Count];
        if (_sourceDataMap.Count > 0)
        {
            var job = new TileRadiationJob
            {
                System = this,
                SourceTree = _sourceTree,
                SourceDataMap = _sourceDataMap,
                WorkItems = workItems,
                Results = results
            };

            _parallel.ProcessNow(job, workItems.Count);
        }

        for (var i = 0; i < workItems.Count; i++)
        {
            var item = workItems[i];
            if (_tileRadiationCache.TryGetValue(item.GridUid, out var samples) &&
                samples.TryGetValue(item.TilePosition, out var existing))
            {
                samples[item.TilePosition] = (results[i], existing.ExpiresAt);
            }
        }
    }

    private readonly record struct TileSamplingWorkItem(
        EntityUid GridUid,
        Vector2i TilePosition,
        TransformComponent GridTransform,
        Vector2 WorldPosition);

    [UsedImplicitly]
    private readonly record struct TileRadiationJob : IParallelRobustJob
    {
        public int BatchSize => 5;
        public required RadiationSystem System { get; init; }
        public required B2DynamicTree<EntityUid> SourceTree { get; init; }
        public required Dictionary<EntityUid, SourceData> SourceDataMap { get; init; }
        public required List<TileSamplingWorkItem> WorkItems { get; init; }
        public required float[] Results { get; init; }

        public void Execute(int index)
        {
            var item = WorkItems[index];
            var nearbySourcesArray = ArrayPool<EntityUid>.Shared.Rent(256);
            var gridList = new List<Entity<MapGridComponent>>(8);

            try
            {
                var queryAabb = new Box2(item.WorldPosition, item.WorldPosition);
                var state = (nearbySourcesArray, 0, SourceTree);
                SourceTree.Query(ref state,
                    static (ref (EntityUid[] arr, int count, B2DynamicTree<EntityUid> tree) tuple,
                        DynamicTree.Proxy proxy) =>
                    {
                        if (tuple.count >= tuple.arr.Length)
                            return true;

                        tuple.arr[tuple.count++] = tuple.tree.GetUserData(proxy);
                        return true;
                    },
                    in queryAabb);

                var rads = 0f;
                foreach (var sourceUid in nearbySourcesArray.AsSpan(0, state.Item2))
                {
                    if (!SourceDataMap.TryGetValue(sourceUid, out var source) ||
                        source.Transform.MapID != item.GridTransform.MapID)
                    {
                        continue;
                    }

                    var delta = source.WorldPosition - item.WorldPosition;
                    if (delta.LengthSquared() > source.MaxRange * source.MaxRange)
                        continue;

                    if (System.Irradiate(
                            source,
                            EntityUid.Invalid,
                            item.GridTransform,
                            item.WorldPosition,
                            saveVisitedTiles: false,
                            gridList) is { ReachedDestination: true } ray)
                    {
                        rads += ray.Rads;
                    }
                }

                Results[index] = rads;
            }
            finally
            {
                ArrayPool<EntityUid>.Shared.Return(nearbySourcesArray);
            }
        }
    }
}
