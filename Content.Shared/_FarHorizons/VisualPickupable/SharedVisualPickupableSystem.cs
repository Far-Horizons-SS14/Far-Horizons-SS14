using Content.Shared.Examine;
using Content.Shared.Hands;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.VisualPickupable;

public abstract class SharedVisualPickupableSystem : EntitySystem
{
    private static EntProtoId _cloneEnt = "VisualPickupableCloneEntity";

    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VisualPickupableComponent, GotEquippedHandEvent>(OnGotPickedUp);
        SubscribeLocalEvent<VisualPickupableComponent, GotUnequippedHandEvent>(OnGotDropped);
        SubscribeLocalEvent<PickupableVisualsComponent, ExamineAttemptEvent>(OnExamineAttempt);
    }

    private void OnExamineAttempt(Entity<PickupableVisualsComponent> ent, ref ExamineAttemptEvent args) => 
        args.Cancel();

    private void OnGotPickedUp(Entity<VisualPickupableComponent> ent, ref GotEquippedHandEvent args)
    {
        var clone = PredictedSpawnAttachedTo(_cloneEnt, Transform(args.User).Coordinates);
        _transform.SetParent(clone, args.User);
        ent.Comp.ClonedVisuals = clone;
        
        var cloneComp = EnsureComp<PickupableVisualsComponent>(clone);
        cloneComp.Source = ent;
        Dirty<PickupableVisualsComponent>((clone, cloneComp));
    }

    private void OnGotDropped(Entity<VisualPickupableComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (ent.Comp.ClonedVisuals == null) return;
        
        PredictedDel(ent.Comp.ClonedVisuals);
        ent.Comp.ClonedVisuals = null;
    }
}