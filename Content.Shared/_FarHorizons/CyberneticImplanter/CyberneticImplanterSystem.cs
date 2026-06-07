using System.Linq;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.CyberneticImplanter;

public sealed class CyberneticImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberneticImplanterComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<CyberneticImplanterComponent, AfterInteractEvent>(OnAfterInteract);
    }

    //using on self
    private void OnUse(Entity<CyberneticImplanterComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryImplant(entity, args.User, args.User))
            args.Handled = true;
    }

    //using on somebody else (or self if thats who they interatcted with)
    private void OnAfterInteract(Entity<CyberneticImplanterComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryImplant(entity, args.Target.Value, args.User))
            args.Handled = true;
    }

    private bool TryImplant(Entity<CyberneticImplanterComponent> entity, EntityUid target, EntityUid user)
    {
        //if statment straight from hell, does all the checks to verify target is valid
        if (!TryComp<BodyComponent>(target, out var bodycomponent) ||
        bodycomponent.Organs == null ||
        bodycomponent.Organs.ContainedEntities == null ||
        !_protoManager.Index<EntityPrototype>(entity.Comp.ImplantedOrgan).TryGetComponent<OrganComponent>(out var ImplantOrganComponent, Factory) || //will cause an exception if yaml is configured incorrectly
        ImplantOrganComponent.Category == null ||
        _protoManager.Index<OrganCategoryPrototype>(ImplantOrganComponent.Category).ConnectsTo == null)
            return false;

        var ConnectsTo = _protoManager.Index<OrganCategoryPrototype>(ImplantOrganComponent.Category).ConnectsTo;

        //are there any organs matching the category in ConnectsTo?
        if (!bodycomponent.Organs.ContainedEntities.Any(p => TryComp<OrganComponent>(p, out var organ) && organ.Category == ConnectsTo))
            return false;

        //ready to go! play sfx and start the doafter
        _audio.PlayPredicted(entity.Comp.ImplantBeginSound, entity, user);

        var doAfterEventArgs = new DoAfterArgs(_entityManager, user, entity.Comp.ActivationTime, new CyberneticImplantDoAfterEvent(), entity, target)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true
        };

        // Server and Client spilt here, dont need the client for the rest of this
        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return false;
        
        _popupSystem.PopupClient(Loc.GetString("comp-cyberneticimplanter-implantstart"), target, PopupType.MediumCaution);

        return true;
    }
}