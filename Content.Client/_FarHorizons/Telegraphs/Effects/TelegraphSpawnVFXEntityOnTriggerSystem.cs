using System.Numerics;
using Content.Shared._FarHorizons.Telegraphs;
using Content.Shared._FarHorizons.Telegraphs.Effects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client._FarHorizons.Telegraphs.Effects;

public sealed partial class TelegraphSpawnVFXEntityOnTriggerSystem : EntitySystem
{
    [Dependency] private TelegraphedAttackSystem _telegraph = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelegraphSpawnVFXEntityOnTriggerComponent, OnTelegraphTriggered>(OnTriggered);
    }

    private void OnTriggered(Entity<TelegraphSpawnVFXEntityOnTriggerComponent> ent, ref OnTelegraphTriggered args)
    {
        var xform = Transform(ent);

        if (xform.GridUid == null ||
            !TryComp<MapGridComponent>(xform.GridUid, out var gridComp) ||
            !TryComp<TelegraphedAttackComponent>(ent, out var telegraphComp) ||
            _telegraph.GetTelegraph((ent, telegraphComp), args.Telegraph) is not {} telegraph ||
            !_transform.TryGetGridTilePosition((ent, xform), out var baseCoords))
            return;
        
        if (ent.Comp.Single)
        {
            SpawnSingleEntity(ent, (xform.GridUid.Value, gridComp), baseCoords);
            return;
        }

        if (!telegraph.Inverse)
            SpawnEntityPerTile(ent, telegraph, (xform.GridUid.Value, gridComp), baseCoords);
    }

    private void SpawnEntityPerTile(Entity<TelegraphSpawnVFXEntityOnTriggerComponent> ent, AttackTelegraph telegraph, Entity<MapGridComponent> grid, Vector2i baseCoords)
    {
        foreach (var tile in telegraph.Tiles)
        {
            var halfTile = grid.Comp.TileSize / 2f;
            var coords = new Vector2(baseCoords.X + tile.X + halfTile, baseCoords.Y + tile.Y + halfTile);
            var mapCoords = new EntityCoordinates(grid.Owner, coords);
            SpawnAtPosition(ent.Comp.Entity, mapCoords);
        }
    }

    private void SpawnSingleEntity(Entity<TelegraphSpawnVFXEntityOnTriggerComponent> ent, Entity<MapGridComponent> grid, Vector2i baseCoords)
    {
        var halfTile = grid.Comp.TileSize / 2f;
        var coords = new Vector2(baseCoords.X + halfTile, baseCoords.Y + halfTile);
        var mapCoords = new EntityCoordinates(grid.Owner, coords);
        SpawnAtPosition(ent.Comp.Entity, mapCoords);
    }
}