using Content.Shared._Starlight.Humanoid.Markings;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Wagging;

namespace Content.Shared._Starlight.Wagging;

public sealed class StarlightWaggingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StarlightMarkingSystem _starlightMarking = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WaggingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WaggingComponent, ApplyOrganMarkingsEvent>(OnMarkingsUpdate);
    }

    private void OnMapInit(Entity<WaggingComponent> ent, ref MapInitEvent args) => UpdateAction(ent);

    private void OnMarkingsUpdate(Entity<WaggingComponent> ent, ref ApplyOrganMarkingsEvent args)
    {
        // SpawnRandomHumanoid creates uninitialized entities and raises this before init
        if (!ent.Comp.Running)
            return;

        UpdateAction(ent);
    }

    private void UpdateAction(Entity<WaggingComponent> ent)
    {
        if (!TryComp<VisualBodyComponent>(ent, out var body)) return;
        if (!_visualBody.TryGatherMarkingsData((ent.Owner, body),
                [ent.Comp.Layer],
                out _,
                out _,
                out var applied))
            return;
        if (!applied.TryGetValue(ent.Comp.Organ, out var markingsSet)) return;

        foreach (var (_, markings) in markingsSet)
        foreach (var marking in markings)
        {
            if (!_starlightMarking.TryGetWaggingId(marking.MarkingId, out _)) continue;
            if (!_actions.GetAction(ent.Comp.ActionEntity).HasValue)
                _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
        }
    }
}