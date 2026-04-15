using System.Linq;
using Content.Shared.Body;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared._FarHorizons.Body;

public sealed partial class VisionOrganSystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blindable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisionOrganRequiredForVisionComponent, CanSeeAttemptEvent>(EyeUserCanSee);
        SubscribeLocalEvent<VisionOrganComponent, OrganGotRemovedEvent>(OnVisionOrganRemoved);
        SubscribeLocalEvent<VisionOrganComponent, OrganGotInsertedEvent>(OnVisionOrganInserted);
    }

    private void OnVisionOrganInserted(Entity<VisionOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!TryComp<BlindableComponent>(args.Target, out var blindable)) return;

        _blindable.SetMinDamage((args.Target, blindable), ent.Comp.MinDamage);
        _blindable.AdjustEyeDamage((args.Target, blindable), ent.Comp.EyeDamage - blindable.EyeDamage);
        _blindable.UpdateIsBlind((args.Target, blindable));
    }

    private void OnVisionOrganRemoved(Entity<VisionOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;

        if (!TryComp<BlindableComponent>(args.Target, out var blindable)) return;

        ent.Comp.MinDamage = blindable.MinDamage;
        ent.Comp.EyeDamage = blindable.EyeDamage;

        _blindable.UpdateIsBlind((args.Target, blindable));
    }

    private void EyeUserCanSee(Entity<VisionOrganRequiredForVisionComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (!Initialized(ent) ||
            args.Cancelled ||
            !TryComp<BodyComponent>(ent, out var body) ||
            body.Organs?.Count == 0) // Body with no organs is either something that didn't finish initializaiton, or some extreme edge case I don't care about
            return;

        if (!body.Organs!.ContainedEntities.Any(HasComp<VisionOrganComponent>))
            args.Cancel();
    }
}