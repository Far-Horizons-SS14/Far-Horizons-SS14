using Content.Shared._Starlight.Chemistry.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.Chemistry;

// This file has substantial FH-sourced modifications
public sealed partial class SLSolutionRegenerationSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnSolutionChanged(Entity<SolutionRegenerationComponent> ent, ref SolutionChangedEvent args)
    {
        if (TerminatingOrDeleted(ent) || LifeStage(ent) < EntityLifeStage.MapInitialized)
            return;

        TryActivateRegeneration(ent, args.Solution.Comp);
    }

    private void TryActivateRegeneration(Entity<SolutionRegenerationComponent> ent, SolutionComponent solution)
    {
        if (_timing.ApplyingState || solution.Solution.AvailableVolume <= FixedPoint2.Zero)
            return;

        EnsureComp<SLActiveSolutionRegenerationComponent>(ent);
    }
}
