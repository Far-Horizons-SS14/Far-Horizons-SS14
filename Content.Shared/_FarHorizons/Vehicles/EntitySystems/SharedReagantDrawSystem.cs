using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Containers;

namespace Content.Shared._FarHorizons.ReagantDraw.EntitySystems;

public sealed class SharedReagantDrain : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReagantDrawComponent, SolutionTransferAttemptEvent>(OnSolutionTransferAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ReagantDrawComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            //if (!comp.Enabled)
                //continue;

            if (_timing.CurTime < comp.NextUpdateTime)
                continue;

            TryUseReagant(uid, comp.DrainRate * frameTime, comp);
            comp.NextUpdateTime += comp.Delay;
        }
    }

    public bool TryUseReagant(EntityUid uid, float value, ReagantDrawComponent? reagantComp = null)
    {
        if(!_net.IsServer) 
            return false;
        if (!Resolve(uid, ref reagantComp, false))
            return false;

        if(!_solutionContainer.ResolveSolution(uid, reagantComp.SolutionContainer, ref reagantComp.Solution, out var solution)) 
            return false;

        UseReagant(uid, value, solution, reagantComp);
        return true;
    }

    private float UseReagant(EntityUid uid, float value, Solution solution, ReagantDrawComponent? reagantComp = null)
    {
        if (value <= 0 || !Resolve(uid, ref reagantComp) || solution.Volume == 0)
            return 0;

        return ChangeReagant(uid, value, solution, reagantComp);
    }

    public float ChangeReagant(EntityUid uid, float value, Solution solution, ReagantDrawComponent? reagantComp = null)
    {
        if (!Resolve(uid, ref reagantComp))
            return 0;
    
        solution.RemoveSolution(value);

        if( _container.TryGetContainer(uid, $"solution@{reagantComp.SolutionContainer}", out var solutionContainer) &&
            solutionContainer is ContainerSlot solutionSlot &&
            solutionSlot.ContainedEntity is { } containedSolution && TryComp<SolutionComponent>(containedSolution, out var solutionComp))
        {
            Dirty(containedSolution, solutionComp);
        }
        return solution.Volume.Float();
    }

    private void OnSolutionTransferAttempt(Entity<ReagantDrawComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        if(ent.Comp.WhitelistedReagants.Count == 0) return;

        var solution = args.SolutionEntity.Comp.Solution;
        if (solution.Contents.Any(sol => !ent.Comp.WhitelistedReagants.Any(req => req.Id == sol.Reagent.ToString())))
        {
            args.Cancel("This solution isn't the right solution!");
            return;
        }
    }
}
