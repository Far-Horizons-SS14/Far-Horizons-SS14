using System.Linq;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Shared._FarHorizons.Body;

public sealed partial class LimbReattachmentandRegrowthSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReattacheableLimbsComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<ReattacheableLimbComponent, ReattachmentAttemptEvent>(OnReattachAttempt);
    }

    private void OnInteract(Entity<ReattacheableLimbsComponent> ent, ref InteractUsingEvent args)
    {
        if(args.Handled 
            || !HasComp<ReattacheableLimbComponent>(args.Used) || !HasComp<VisualOrganComponent>(args.Used)
            || !TryComp<BodyComponent>(ent.Owner, out var body) || body.Organs == null || !TryComp<OrganComponent>(args.Used, out var organComp1))
                return;
                
        var attachable = !body.Organs.ContainedEntities.ToArray().Any(organId =>
            TryComp<OrganComponent>(organId, out var organComp2) &&
            organComp2.Category == organComp1.Category);

        if(attachable)
        {
            var ev = new ReattachmentAttemptEvent();
            var installDoAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.ReattachTime, ev, args.Used, ent.Owner)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = true
            };
            _doAfter.TryStartDoAfter(installDoAfter);
        }
        else
            _popupSystem.PopupCursor("Can't reattach this limb.");

        args.Handled = true;
    }

    private void OnReattachAttempt(EntityUid uid, ReattacheableLimbComponent comp, ref ReattachmentAttemptEvent args)
    {
        if(!TryComp<BodyComponent>(args.Target, out var body) || body.Organs == null)
            return;
        _container.Insert(uid, body.Organs);
        _popupSystem.PopupCursor("You reattached this limb successfully.");
    }
}