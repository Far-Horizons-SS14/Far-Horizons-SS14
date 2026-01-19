using Content.Server._FarHorizons.Tools.FloorBuffer.Components;
using Content.Server.Decals;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Decals;
using Content.Shared.Fluids.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using System.Linq;
using System.Numerics;

namespace Content.Server._FarHorizons.Tools.FloorBuffer.Systems;

public sealed class FloorBufferSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    static readonly public ProtoId<ReagentPrototype> ReplacementReagent = "Water";
    public override void Initialize()
    {
        base.Initialize();
    }
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        var query = EntityQueryEnumerator<FloorBufferComponent, TransformComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var floorComp, out var xForm, out var Phys))
        {
            if (!floorComp.Enabled)
                continue;
            if(!TryComp<MapGridComponent>(xForm.GridUid, out var grid))
                continue;
            
            if ((Phys.LinearVelocity.Equals(Vector2.Zero) && Phys.AngularVelocity.Equals(0f)) || Phys.BodyStatus == BodyStatus.InAir)
                continue;

            var tile = _map.GetTileRef(xForm.GridUid.Value, grid, xForm.Coordinates);
            CleanDecalssandPuddles(tile, grid);
        }
    }

    private void CleanDecalssandPuddles(TileRef tile, MapGridComponent grid)
    {
        if(TryComp<DecalGridComponent>(tile.GridUid, out var decalGrid))
        {
            var decals = _decals.GetDecalsIntersecting(tile.GridUid, _lookup.GetLocalBounds(tile, grid.TileSize).Enlarged(0.5f).Translated(new Vector2(-0.5f,-0.5f)));
            foreach(var decal in decals)
            {
                if(!decal.Decal.Cleanable)
                    continue;

                _decals.RemoveDecal(tile.GridUid, decal.Index, decalGrid);
            }
        }
        var entities = _lookup.GetLocalEntitiesIntersecting(tile, 0f).ToArray();
        foreach(var entity in entities)
        {
            if(!TryComp<PuddleComponent>(entity, out var puddleComp) 
                || !_solutionContainer.TryGetSolution(entity, puddleComp.SolutionName, out var solutionComp, out var solution))
                continue;
            
            var replaceTotal = _solutionContainer.SplitSolutionWithout(solutionComp.Value, solution.Volume, ReplacementReagent);
            _solutionContainer.TryAddSolution(solutionComp.Value, new Solution(ReplacementReagent, replaceTotal.Volume/2));
        }
    }
}