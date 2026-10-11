using System.Numerics;
using Content.Shared._FarHorizons.Fluids.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Fluids.Components;
using Content.Shared.Gravity;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Fluids;

public abstract partial class SharedFluidFootprintSystem
{
    [SubscribeLocalEvent]
    private void OnEndCollide(Entity<FluidFootprintSpreaderComponent> ent, ref EndCollideEvent args)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(args.OtherEntity) ||
            (TryComp<BuckleComponent>(ent, out var buckle) && buckle.BuckledTo != null) ||
            HasComp<KnockedDownComponent>(ent) ||
            !TryComp<GravityAffectedComponent>(ent, out var gravAffected) ||
            gravAffected.Weightless ||
            !TryComp<FluidFootprintSourceComponent>(args.OtherEntity, out var source) ||
            !TryComp<PuddleComponent>(args.OtherEntity, out var puddle) ||
            !Solution.ResolveSolution(args.OtherEntity, puddle.SolutionName, ref puddle.Solution) ||
            puddle.Solution == null ||
            !_timing.IsFirstTimePredicted)
            return;
        
        var solution = puddle.Solution.Value.Comp.Solution;
        if (!solution.FootprintEligible(ProtoMan)) return;
        
        var numFootprints = ResolveNumFootprints((args.OtherEntity, source, puddle));

        if (numFootprints <= 0)
            return;

        var activeSpreader = EnsureComp<ActiveFluidFootprintSpreaderComponent>(ent);
        if (numFootprints <= activeSpreader.RemainingFootprints)
            return;
        
        var removeQt = source.TakeSolutionUnits / solution.Contents.Count;
        Solution.RemoveEachReagent(puddle.Solution.Value, removeQt);

        var color = solution.GetColor(ProtoMan);

        activeSpreader.RemainingFootprints = numFootprints;
        activeSpreader.StopAt = _timing.CurTime + source.StopAfter;
        activeSpreader.LastPosition = TransformSys.GetMapCoordinates(ent);
        activeSpreader.Color = color;
        activeSpreader.OpacityStep = 1f / (numFootprints + 1);
        activeSpreader.FootprintRate = ent.Comp.FootprintRate;
        activeSpreader.StepSpacing = ent.Comp.StepSpacing;
        activeSpreader.FootprintSize = ent.Comp.FootprintSize;
        activeSpreader.Footprint = ent.Comp.Footprint;
        activeSpreader.LateralOffset = ent.Comp.LateralOffset;
    }

    [SubscribeLocalEvent]
    private void OnEndCollideDragged(Entity<PullableComponent> ent, ref EndCollideEvent args)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(args.OtherEntity) ||
            !ent.Comp.BeingPulled ||
            (HasComp<FluidFootprintSpreaderComponent>(ent) && !HasComp<KnockedDownComponent>(ent)) ||
            !TryComp<GravityAffectedComponent>(ent, out var gravAffected) ||
            gravAffected.Weightless ||
            !TryComp<FluidFootprintSourceComponent>(args.OtherEntity, out var source) ||
            !TryComp<PuddleComponent>(args.OtherEntity, out var puddle) ||
            !Solution.ResolveSolution(args.OtherEntity, puddle.SolutionName, ref puddle.Solution) ||
            puddle.Solution == null ||
            !_timing.IsFirstTimePredicted)
            return;
        
        var solution = puddle.Solution.Value.Comp.Solution;
        if (!solution.FootprintEligible(ProtoMan)) return;
        
        var numFootprints = ResolveNumFootprints((args.OtherEntity, source, puddle));

        if (numFootprints <= 0)
            return;

        var activeSpreader = EnsureComp<ActiveFluidFootprintSpreaderComponent>(ent);
        if (numFootprints <= activeSpreader.RemainingFootprints)
            return;
        
        var removeQt = source.TakeSolutionUnits / solution.Contents.Count;
        Solution.RemoveEachReagent(puddle.Solution.Value, removeQt);

        var color = solution.GetColor(ProtoMan);

        activeSpreader.RemainingFootprints = numFootprints;
        activeSpreader.StopAt = _timing.CurTime + source.StopAfter;
        activeSpreader.LastPosition = TransformSys.GetMapCoordinates(ent);
        activeSpreader.Color = color;
        activeSpreader.OpacityStep = 1f / (numFootprints + 1);
        activeSpreader.FootprintRate = source.DraggingFootprintRate;
        activeSpreader.StepSpacing = source.DraggingStepSpacing;
        activeSpreader.FootprintSize = source.DraggingFootprintSize;
        activeSpreader.Footprint = source.DraggingFootprint;
        activeSpreader.LateralOffset = 0f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted) return;

        var query = EntityQueryEnumerator<ActiveFluidFootprintSpreaderComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var activeSpreader, out var xform))
        {
            if (_timing.CurTime < activeSpreader.NextStep)
                continue;

            if (activeSpreader.StopAt <= _timing.CurTime ||
                activeSpreader.RemainingFootprints <= 0)
            {
                RemCompDeferred<ActiveFluidFootprintSpreaderComponent>(uid);
                continue;
            }

            if (activeSpreader.FootprintRate == null ||
                activeSpreader.StepSpacing == null ||
                activeSpreader.Footprint == null ||
                activeSpreader.LateralOffset == null)
                continue;
            
            activeSpreader.NextStep = _timing.CurTime + activeSpreader.FootprintRate.Value;

            var currentPos = TransformSys.GetMapCoordinates(uid, xform);
            var lastPos = activeSpreader.LastPosition ?? currentPos;

            if (currentPos.MapId != lastPos.MapId)
            {
                activeSpreader.LastPosition = currentPos;
                continue;
            }

            var distance = (currentPos.Position - lastPos.Position).Length();
            if (distance < 0.1f)
                continue;
            
            var stepSpacing = activeSpreader.StepSpacing.Value;
            var totalSteps = Math.Min(activeSpreader.RemainingFootprints, (int) Math.Floor(distance / stepSpacing));

            if (totalSteps <= 0)
                continue;
            
            activeSpreader.LastPosition = currentPos;
            
            var pathVector = currentPos.Position - lastPos.Position;
            var angle = pathVector.ToAngle();

            var ev = new BootFootprintModifyEvent(activeSpreader.Footprint.Value);
            RaiseLocalEvent(uid, ref ev);
            var footprint = ev.Footprint;

            for (var i = 1; i <= totalSteps; i++)
            {
                var t = i / (float) totalSteps;
                var interpolatedPos = Vector2.Lerp(lastPos.Position, currentPos.Position, t);

                var mapCoords = new MapCoordinates(interpolatedPos, currentPos.MapId);
                DoStep((uid, activeSpreader), mapCoords, angle, footprint);

                activeSpreader.RemainingFootprints--;
                if (activeSpreader.RemainingFootprints <= 0)
                    break;
            }
        }
    }

    public void DoStep(Entity<ActiveFluidFootprintSpreaderComponent> ent, MapCoordinates mapCoords, Angle angle, ProtoId<FootprintTypePrototype> footprintType)
    {
        if (!_map.TryFindGridAt(mapCoords, out var gridUid, out var gridComp))
            return;
        
        if (ent.Comp.FootprintRate == null ||
            ent.Comp.StepSpacing == null ||
            ent.Comp.Footprint == null ||
            ent.Comp.LateralOffset == null)
            return;
        
        var proto = ProtoMan.Index(ent.Comp.Footprint.Value);
        var finalPos = mapCoords.Position;

        if (proto.Alternating)
        {
            var moveDir = angle.ToVec();
            var leftPerpendicular = new Vector2(-moveDir.Y, moveDir.X).Normalized();

            var sideMultiplier = ent.Comp.Left ? 1f : -1f;
            var offsetAmount = ent.Comp.LateralOffset.Value;

            finalPos += leftPerpendicular * (offsetAmount * sideMultiplier);
        }

        mapCoords = new MapCoordinates(finalPos, mapCoords.MapId);
        
        var gridLocalCoords = TransformSys.ToCoordinates(gridUid, mapCoords);

        var tileIndices = _map.TileIndicesFor((gridUid, gridComp), gridLocalCoords);
        var tile = ResolveFootprintTile((gridUid, gridComp), tileIndices);

        var tileCenter = _map.GridTileToLocal(gridUid, gridComp, tileIndices);
        var relativePos = gridLocalCoords.Position - tileCenter.Position;

        var flipped = proto.Alternating && !ent.Comp.Left;

        if (tile != null)
        {
            tile!.Value.Comp.AddFootprint(relativePos, angle, footprintType, ent.Comp.FootprintSize, ent.Comp.Color, flipped, ent.Comp.Opacity);
            UpdateSprite(tile.Value);
            Dirty(tile.Value);
        }
        
        var opacity = MathF.Max(0f, ent.Comp.Opacity - ent.Comp.OpacityStep);
        ent.Comp.Opacity = opacity;
        if (proto.Alternating)
            ent.Comp.Left = !ent.Comp.Left;
    }

    public int ResolveNumFootprints(Entity<FluidFootprintSourceComponent, PuddleComponent> ent)
    {
        if (ent.Comp2.Solution == null) return 0;

        var solution = ent.Comp2.Solution.Value.Comp.Solution;

        if (solution.Volume < ent.Comp1.MinUnitsForFootPrint)
            return 0;
        
        var volumeBonus = (int)((solution.Volume - ent.Comp1.MinUnitsForFootPrint) * ent.Comp1.BonusPerUnit);
        return ent.Comp1.MinFootprints + volumeBonus;
    }
}