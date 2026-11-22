using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Radiation.Systems;

// This file was added by Far Horizons as a place for Funky-Station-originating
// code for grid-based radiation queries, after we moved it out of the Wizden-
// originating code in RadiationSystem after a refactor on the Wizden side.

public partial class RadiationSystem
{
    [Dependency] private IGameTiming _gameTiming = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    private readonly Dictionary<EntityUid, Dictionary<Vector2i, (float Radiation, TimeSpan ExpiresAt)>>
        _tileRadiationCache = new();

    private void InitializeTileSampling()
    {
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

    private void UpdateTileRadiationCache(bool debug)
    {
        var now = _gameTiming.CurTime;
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
                var rads = 0f;
                foreach (var source in _sources)
                {
                    if (Irradiate(source, EntityUid.Invalid, gridTransform, worldPosition, debug) is { } ray &&
                        ray.Rads > 0.001f)
                    {
                        rads += ray.Rads;
                    }
                }

                samples[tilePos] = (rads, expiresAt);
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
    }
}
