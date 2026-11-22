using System.Linq;
using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Telegraphs;

public abstract partial class SharedTelegraphedAttackSystem
{
    public TimeSpan GetTotalDuration(Entity<TelegraphedAttackComponent> ent) => ent.Comp.Telegraphs.Max(p => p.EffectDelay);

    public AttackTelegraph? GetTelegraph(Entity<TelegraphedAttackComponent> ent, int telegraphId) => ent.Comp.Telegraphs.ElementAt(telegraphId);

    public List<AttackTelegraph> GetCurrentlyVisibleTelegraphs(Entity<TelegraphedAttackComponent> ent)
    {
        var result = new List<AttackTelegraph>();
        if (!ent.Comp.IsActive) return result;

        result = [.. ent.Comp.Telegraphs.Where(p => Timing.CurTime < ent.Comp.StartTime + p.EffectDelay && Timing.CurTime > ent.Comp.StartTime + p.DisplayDelay)];

        return result;
    }

    public float GetTelegraphProgress(Entity<TelegraphedAttackComponent> ent, AttackTelegraph telegraph)
    {
        var startTime = ent.Comp.StartTime + telegraph.DisplayDelay;
        var endTime = ent.Comp.StartTime + telegraph.EffectDelay;
        var currentTime = Timing.CurTime;

        if (endTime <= startTime)
            return 1.0f;
        
        var progress = !ent.Comp.Continuous
            ? (currentTime - startTime) / (endTime - startTime)
            : (currentTime - ent.Comp.StartTime) / (endTime - ent.Comp.StartTime);
        return Math.Clamp((float)progress, 0f, 1f);
    }

    public HashSet<Entity<T>> FindAffectedComponents<T>(Entity<TelegraphedAttackComponent> ent, AttackTelegraph telegraph) where T : IComponent => 
        telegraph.Inverse
            ? FindAffectedComponentsInversed<T>(ent, telegraph.Tiles)
            : FindAffectedComponentsDirect<T>(ent, telegraph.Tiles);

    public HashSet<Entity<T>> FindAffectedComponentsDirect<T>(Entity<TelegraphedAttackComponent> ent, HashSet<Vector2i> tiles) where T : IComponent
    {
        var result = new HashSet<Entity<T>>();
        var xform = Transform(ent);

        if (xform.GridUid == null ||
            !TryComp<MapGridComponent>(xform.GridUid.Value, out var grid) ||
            !_transform.TryGetMapOrGridCoordinates(ent, out var gridPos, xform) ||
            !_transform.TryGetGridTilePosition((ent, xform), out var tilePos, grid))
            return result;

        var lookupRange = tiles.Max(p => (p).Length) + 1; // Me in school: how a geometry would ever be useful in life. Anyways, I'm kinda just hoping this approach captures everyone and no weird edge cases happen
        var entsInRange = new HashSet<Entity<T>>();
        _lookup.GetEntitiesInRange<T>(gridPos.Value, lookupRange, entsInRange, LookupFlags.All);

        foreach (var nearbyEnt in entsInRange)
        {
            if (ent.Comp.IgnoreTargets.Contains(nearbyEnt)) continue;

            var nearbyXform = Transform(nearbyEnt);

            if (xform.GridUid != nearbyXform.GridUid ||
                !_transform.TryGetGridTilePosition((nearbyEnt, nearbyXform), out var nearbyTilePos) ||
                !tiles.Contains(nearbyTilePos - tilePos))
                continue;

            result.Add(nearbyEnt);
        }

        return result;
    }

    public HashSet<Entity<T>> FindAffectedComponentsInversed<T>(Entity<TelegraphedAttackComponent> ent, HashSet<Vector2i> tiles) where T : IComponent
    {
        var result = new HashSet<Entity<T>>();
        var xform = Transform(ent);

        if (xform.GridUid == null ||
            !TryComp<MapGridComponent>(xform.GridUid.Value, out var grid) ||
            !_transform.TryGetMapOrGridCoordinates(ent, out var gridPos, xform) ||
            !_transform.TryGetGridTilePosition((ent, xform), out var tilePos, grid))
            return result;

        var lookupRange = tiles.Max(p => p.Length) + 1; // Me in school: how a geometry would ever be useful in life. Anyways, I'm kinda just hoping this approach captures everyone and no weird edge cases happen
        var entsOnGrid = new HashSet<Entity<T>>();
        _lookup.GetChildEntities<T>(xform.GridUid.Value, entsOnGrid);

        foreach (var gridEnt in entsOnGrid)
        {
            if (ent.Comp.IgnoreTargets.Contains(gridEnt)) continue;

            var gridXform = Transform(gridEnt);

            if (!_transform.TryGetGridTilePosition((gridEnt, gridXform), out var gridTilePos) ||
                tiles.Contains(gridTilePos - tilePos))
                continue;

            result.Add(gridEnt);
        }

        return result;
    }

    public Entity<TelegraphedAttackComponent>? SpawnTelegraphAt(EntProtoId telegraph, EntityUid target)
    {
        if (!_transform.TryGetMapOrGridCoordinates(target, out var coords)) return null;

        var spawned = SpawnAttachedTo(telegraph, coords.Value);

        return 
            !TryComp<TelegraphedAttackComponent>(spawned, out var telegraphComp) ? 
                null : 
                (Entity<TelegraphedAttackComponent>?)(spawned, telegraphComp);
    }

    public Entity<TelegraphedAttackComponent>? SpawnTelegraphAttachedTo(EntProtoId telegraph, EntityUid target)
    {
        var coords = new EntityCoordinates(target, Vector2.Zero);
        var spawned = SpawnAttachedTo(telegraph, coords);

        return 
            !TryComp<TelegraphedAttackComponent>(spawned, out var telegraphComp) ? 
                null : 
                (Entity<TelegraphedAttackComponent>?)(spawned, telegraphComp);
    }
}