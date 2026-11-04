using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Shared._FarHorizons.Fluids;
using Content.Shared._FarHorizons.Fluids.Components;
using Content.Shared._FarHorizons.Tools.FloorBuffer.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Physics.Events;

public sealed partial class FluidFootprintSystem : SharedFluidFootprintSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => ClearCache());
    }

    [SubscribeLocalEvent]
    private void OnContainerCollide(Entity<FluidFootprintContainerComponent> ent, ref StartCollideEvent args)
    {
        Entity<SolutionComponent>? solutionEnt = null;
        Solution? solution = null;

        if (HasComp<SmokeComponent>(args.OtherEntity))
        {
            if (!Solution.ResolveSolution(args.OtherEntity, SmokeComponent.SolutionName, ref solutionEnt, out solution))
                return;
        }
        else if (HasComp<VaporComponent>(args.OtherEntity) && TryComp<SolutionComponent>(args.OtherEntity, out var solutionComp))
        {
            solutionEnt = (args.OtherEntity, solutionComp);
            solution = solutionComp.Solution;
        }
        else if (TryComp<FloorBufferComponent>(args.OtherEntity, out var floorBuffer) && floorBuffer.Enabled)
        {
            if (!Solution.ResolveSolution(args.OtherEntity, floorBuffer.SolutionContainer, ref solutionEnt, out solution))
                return;
        }

        if (solution == null ||
            !solution.Any() ||
            !solution.FootprintCleanEligible(ProtoMan))
            return;
        
        if (ent.Comp.CleanEffect != null &&
            TransformSys.TryGetMapOrGridCoordinates(ent, out var pos))
            PredictedSpawnAtPosition(ent.Comp.CleanEffect, pos.Value);
        
        QueueDel(ent);
    }
}
