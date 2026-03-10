using Content.Shared._FarHorizons.GenericFieldGenerator.Components;
using Content.Shared.Destructible;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Server._FarHorizons.GenericFieldGenerator.EntitySystems;

public sealed class GenericFieldSystem : EntitySystem
{
    [Dependency] private readonly GenericFieldGeneratorSystem _genericgen = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenericFieldComponent, DestructionEventArgs>(OnDestructionEvent);
    }

    private void OnDestructionEvent(Entity<GenericFieldComponent> field, ref DestructionEventArgs args)
    {
        if (field.Comp.SourceGen == null)
            return;
        TempTileCleanup(field);
        _genericgen.FieldDestroyed(field.Comp.SourceGen.Value);
    }

    public void TempTileCleanup(Entity<GenericFieldComponent> field)
    {
        if (field.Comp.TempTile)
        {
            if (!_tiledef.TryGetDefinition("Space", out var tileDef))
                return;

            _tile.ReplaceTile(field.Comp.Tileref, (ContentTileDefinition)tileDef, field.Comp.GridUid, field.Comp.MapGrid);
        }
    }
}
//Space