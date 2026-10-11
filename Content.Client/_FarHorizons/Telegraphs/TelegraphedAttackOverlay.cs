using System.Numerics;
using Content.Shared._FarHorizons.Telegraphs;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.Telegraphs;

public sealed class TelegraphedAttackOverlay : Overlay
{
    private readonly IEntityManager _entMan;
    private readonly IPrototypeManager _protoMan;
    private readonly SharedTransformSystem _transform;
    private readonly EntityLookupSystem _lookup;
    private readonly TelegraphedAttackSystem _telegraph;

    private readonly HashSet<Entity<TransformComponent, TelegraphedAttackComponent>> _visualizers = new();

    private static readonly ProtoId<ShaderPrototype> _shaderProto = "TelegraphedAttackProgress";
    private static readonly ProtoId<ShaderPrototype> _shaderContinuousProto = "TelegraphedAttackContinuous";
    private readonly ShaderInstance _shader;
    private readonly ShaderInstance _shaderContinuous;

    private Color _hostileTelegraphColor;
    private Color _utilityTelegraphColor;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public TelegraphedAttackOverlay(IEntityManager entMan, IPrototypeManager protoMan)
    {
        _entMan = entMan;
        _protoMan = protoMan;
        _transform = _entMan.System<SharedTransformSystem>();
        _lookup = _entMan.System<EntityLookupSystem>();
        _telegraph = _entMan.System<TelegraphedAttackSystem>();

        _shader = _protoMan.Index(_shaderProto).Instance();
        _shaderContinuous = _protoMan.Index(_shaderContinuousProto).Instance();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _visualizers.Clear();
        _lookup.GetEntitiesOnMap(args.MapId, _visualizers);
        _visualizers.RemoveWhere(p => !p.Comp2.IsActive);

        return _visualizers.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        foreach (var (uid, xform, telegraph) in _visualizers)
        {
            if (xform.GridUid == null || 
                !_entMan.TryGetComponent<MapGridComponent>(xform.GridUid.Value, out var grid) ||
                !_transform.TryGetGridTilePosition((uid, xform), out var tilePos, grid))
                continue;

            var gridXform = _entMan.GetComponent<TransformComponent>(xform.GridUid.Value);
            var (_, _, gridMatrix, invMatrix) = _transform.GetWorldPositionRotationMatrixWithInv(gridXform);
            handle.SetTransform(gridMatrix);

            var telegraphColor = GetColor(telegraph.Kind);
            var telegraphColorVector = new Vector3(telegraphColor.R, telegraphColor.G, telegraphColor.B);

            foreach (var part in _telegraph.GetCurrentlyVisibleTelegraphs((uid, telegraph)))
            {
                var progress = _telegraph.GetTelegraphProgress((uid, telegraph), part);
                var partShader = telegraph.Continuous ? _shaderContinuous.Duplicate() : _shader.Duplicate();
                partShader.SetParameter("progress", progress);
                partShader.SetParameter("baseColor", telegraphColorVector);
                partShader.MakeImmutable();

                handle.UseShader(partShader);
                if (!part.Inverse)
                    DrawTiles(handle, xform, grid, part, tilePos);
                else
                    DrawInverseTiles(handle, xform, grid, part, tilePos, args.WorldBounds, invMatrix);
            }
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTiles(DrawingHandleWorld handle, TransformComponent xform, MapGridComponent grid, AttackTelegraph part, Vector2i tilePos)
    {
        foreach (var tileOffset in part.Tiles)
        {
            var targetTilePos = tilePos + tileOffset;
            var localTileX = targetTilePos.X * grid.TileSize;
            var localTileY = targetTilePos.Y * grid.TileSize;

            var localBounds = new Box2(localTileX, localTileY, localTileX + grid.TileSize, localTileY + grid.TileSize);

            
            handle.DrawRect(localBounds, Color.White);
        }
    }

    private void DrawInverseTiles(DrawingHandleWorld handle, TransformComponent xform, MapGridComponent grid, AttackTelegraph part, Vector2i tilePos, Box2Rotated worldBounds, Matrix3x2 invMatrix)
    {
        Box2 localBox = invMatrix.TransformBox(worldBounds.CalcBoundingBox());

        var minTileX = (int)MathF.Floor(localBox.Left / grid.TileSize);
        var maxTileX = (int)MathF.Ceiling(localBox.Right / grid.TileSize);
        var minTileY = (int)MathF.Floor(localBox.Bottom / grid.TileSize);
        var maxTileY = (int)MathF.Ceiling(localBox.Top / grid.TileSize);

        for (var x = minTileX; x <= maxTileX; x++)
        {
            for (var y = minTileY; y <= maxTileY; y++)
            {
                var currentGridTile = new Vector2i(x, y);

                var relativeOffset = currentGridTile - tilePos;

                if (part.Tiles.Contains(relativeOffset))
                    continue;

                var localTileX = currentGridTile.X * grid.TileSize;
                var localTileY = currentGridTile.Y * grid.TileSize;

                var localBounds = new Box2(
                    localTileX, 
                    localTileY, 
                    localTileX + grid.TileSize, 
                    localTileY + grid.TileSize
                );

                handle.DrawRect(localBounds, Color.White);
            }
        }
    }

    private Color GetColor(TelegraphKind kind) =>
        kind switch
        {
            TelegraphKind.Hostile => _hostileTelegraphColor,
            TelegraphKind.Utility => _utilityTelegraphColor,
            _ => Color.Black,
        };

    public void SetColors(string hostile, string utility)
    {
        _hostileTelegraphColor = Color.FromHex(hostile);
        _utilityTelegraphColor = Color.FromHex(utility);
    }
}