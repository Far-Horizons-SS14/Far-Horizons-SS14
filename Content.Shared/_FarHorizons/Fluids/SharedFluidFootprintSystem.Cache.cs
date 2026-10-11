using Content.Shared._FarHorizons.Fluids.Components;
using Robust.Shared.Map.Components;

namespace Content.Shared._FarHorizons.Fluids;

public abstract partial class SharedFluidFootprintSystem
{
    protected Dictionary<(EntityUid, Vector2i), Entity<FluidFootprintContainerComponent>> Cache = new();

    [SubscribeLocalEvent]
    private void OnContainerShutdown(Entity<FluidFootprintContainerComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.Footprints.Clear();

        foreach (var (key, value) in Cache)
            if (value.Owner == ent.Owner)
                Cache.Remove(key);
    }
    
    public Entity<FluidFootprintContainerComponent>? ResolveFootprintTile(Entity<MapGridComponent> grid, Vector2i coord)
    {
        var key = (grid, coord);

        if (Cache.TryGetValue(key, out var cached))
        {
            if (!TerminatingOrDeleted(cached.Owner) &&
                !EntityManager.IsQueuedForDeletion(cached.Owner))
                return cached;
            
            Cache.Remove(key);
        }
        
        Entity<FluidFootprintContainerComponent>? tile = null;

        var query = EntityQueryEnumerator<FluidFootprintContainerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var container, out var xform))
        {
            if (xform.GridUid != grid ||
                !TransformSys.TryGetGridTilePosition((uid, xform), out var pos) ||
                pos != coord)
                continue;
            
            tile = (uid, container);
            break;
        }

        if (tile == null &&
            _net.IsServer) // I just can't make it predicted no matter what
        {
            var tileCenter = _map.GridTileToLocal(grid, grid.Comp, coord);
            var spawned = Spawn(CONTAINER_ENTITY, tileCenter);
            TransformSys.SetParent(spawned, grid.Owner);
            var comp = EnsureComp<FluidFootprintContainerComponent>(spawned);
            tile = (spawned, comp);
        }

        if (tile != null)
            Cache.Add(key, tile.Value);

        return tile;
    }

    protected void ClearCache() => Cache.Clear();
}