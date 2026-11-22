using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Timing;
using Robust.Shared.Containers;

namespace Content.Shared._FarHorizons.ReagentDraw;

public sealed partial class SharedReagentDrawSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReagentDrawComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ReagentDrawComponent, SolutionTransferAttemptEvent>(OnSolutionTransferAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ReagentDrawComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled)
                continue;

            if (_timing.CurTime < comp.NextUpdateTime)
                continue;
            
            comp.NextUpdateTime = _timing.CurTime + comp.Delay;
            if(TryUseReagent(uid, comp.DrainRate, comp))
                continue;
            
            var ev = new ReagentContainerSlotEmptyEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void OnMapInit(Entity<ReagentDrawComponent> ent, ref MapInitEvent args) => 
        ent.Comp.NextUpdateTime = _timing.CurTime + ent.Comp.Delay;

    public bool TryUseReagent(EntityUid uid, float value, ReagentDrawComponent? reagentComp = null)
    {
        if (!Resolve(uid, ref reagentComp, false))
            return false;

        if(!_solutionContainer.ResolveSolution(uid, reagentComp.SolutionContainer, ref reagentComp.Solution, out var solution)
        || value > solution.Volume) 
            return false;

        UseReagent(uid, value, solution, reagentComp);
        return true;
    }

    private float UseReagent(EntityUid uid, float value, Solution solution, ReagentDrawComponent? reagentComp = null)
    {
        if (value <= 0 || !Resolve(uid, ref reagentComp) || solution.Volume == 0)
            return 0;

        return ChangeReagent(uid, value, solution, reagentComp);
    }

    public float ChangeReagent(EntityUid uid, float value, Solution solution, ReagentDrawComponent? reagentComp = null)
    {
        if (!Resolve(uid, ref reagentComp))
            return 0;
    
        solution.RemoveSolution(value);

        if( _container.TryGetContainer(uid, $"solution@{reagentComp.SolutionContainer}", out var solutionContainer) &&
            solutionContainer is ContainerSlot solutionSlot &&
            solutionSlot.ContainedEntity is { } containedSolution && TryComp<SolutionComponent>(containedSolution, out var solutionComp))
        {
            Dirty(containedSolution, solutionComp);
        }

        var ev = new ReagentChangedEvent(solution.Volume.Float(), solution.MaxVolume.Float());
        var ev2 = new SolutionChangedEvent();
        RaiseLocalEvent(uid, ref ev);
        RaiseLocalEvent(uid, ref ev2);

        return solution.Volume.Float();
    }

    public bool HasDrawReagent(
        EntityUid uid,
        ReagentDrawComponent? reagentComp = null)
    {
        if (!Resolve(uid, ref reagentComp, false))
            return true;

        return HasReagent(uid, reagentComp.DrainRate, reagentComp);
    }
    
    public bool HasReagent(EntityUid uid, float charge, ReagentDrawComponent reagentComp)
    {
        if(!_solutionContainer.ResolveSolution(uid, reagentComp.SolutionContainer, ref reagentComp.Solution, out var solution)) 
            return false;

        if (solution.Volume < charge)
            return false;

        return true;
    }

    private void OnSolutionTransferAttempt(Entity<ReagentDrawComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        if(ent.Comp.WhitelistedReagents.Count == 0) return;

        var solution = args.SolutionEntity.Comp.Solution;
        if (solution.Contents.Any(sol => !ent.Comp.WhitelistedReagents.Any(req => req.Id == sol.Reagent.ToString())))
        {
            args.Cancel("This solution isn't the right solution!");
            return;
        }
    }

    public void SetDrawEnabled(Entity<ReagentDrawComponent?> ent, bool enabled)
    {
        if (Resolve(ent, ref ent.Comp, false) && ent.Comp.Enabled != enabled)
        {
            ent.Comp.Enabled = enabled;
            Dirty(ent, ent.Comp);
        }
    } 
}
