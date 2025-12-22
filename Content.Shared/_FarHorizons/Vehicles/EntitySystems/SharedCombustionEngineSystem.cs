using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared._FarHorizons.CombustionEngine;

namespace Content.Shared._FarHorizons.CombustionEngine.EntitySystems;

public sealed class SharedCombustionEngine : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CombustionEngineComponent, SolutionTransferAttemptEvent>(OnSolutionTransferAttempt);
    }

    private void OnSolutionTransferAttempt(Entity<CombustionEngineComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        var solution = args.SolutionEntity.Comp.Solution;
        if (!solution.Contents.Any(sol => ent.Comp.Fuel.Id == sol.Reagent.ToString()))
            args.Cancel("This solution isn't fuel!");
    }
}
