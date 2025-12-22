using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.ReagantDrain.EntitySystems;

public sealed class SharedReagantDrain : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReagantDrainComponent, SolutionTransferAttemptEvent>(OnSolutionTransferAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ReagantDrainComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            //if (!comp.Enabled)
                //continue;

            if (_timing.CurTime < comp.NextUpdateTime)
                continue;

            UseFuel(uid, comp);
            comp.NextUpdateTime += comp.Delay;
        }
    }

    private void UseFuel(EntityUid uid, ReagantDrainComponent engineComp)
    {
        if(!_net.IsServer) return;
        _solutionContainer.ResolveSolution(uid, engineComp.SolutionContainer, ref engineComp.Solution, out var solution);
        if(solution == null) return;
        
        var totalFuel = solution.Contents.FirstOrDefault(sol => engineComp.Fuel.Id == sol.Reagent.ToString());
    }

    private void OnSolutionTransferAttempt(Entity<ReagantDrainComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        var solution = args.SolutionEntity.Comp.Solution;
        if (!solution.Contents.Any(sol => ent.Comp.Fuel.Id == sol.Reagent.ToString()))
        {
            args.Cancel("This solution isn't the right solution!");
            return;
        }
        if (solution.Contents.Any(sol => ent.Comp.Fuel.Id != sol.Reagent.ToString()))
        {
            args.Cancel("This solution can't be use it's mixed!");
            return;
        }
    }
}
