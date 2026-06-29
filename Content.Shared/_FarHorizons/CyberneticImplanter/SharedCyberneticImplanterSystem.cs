using System.Linq;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.CyberneticImplanter;

public sealed class SharedCyberneticImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _visualizer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberneticImplanterComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<CyberneticImplanterComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<UsedCyberneticImplanterComponent, ExaminedEvent>(OnExamineUsed);
    }

    private void OnExamineUsed(EntityUid entity, UsedCyberneticImplanterComponent component, ExaminedEvent args) //used to show what organ was destroyed after an implant
    {
        if (!args.IsInDetailsRange)
            return;

        if (component.Organ != null)
            args.PushMarkup(Loc.GetString("comp-usedcyberneticimplanter-examine", ("species", component.Species), ("organ", component.Organ)));
    }

    //using on self`
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
        //most of this doesnt need to be done on the client, but seperating it isnt neccessary
        //if statment straight from hell, does all the checks to verify target is valid
        if (!HasComp<HumanoidProfileComponent>(target) ||
        !TryComp<BodyComponent>(target, out var bodycomponent) ||
        bodycomponent.Organs == null ||
        bodycomponent.Organs.ContainedEntities == null ||
        !_protoManager.Index<EntityPrototype>(entity.Comp.ImplantedOrgan).TryGetComponent<OrganComponent>(out var implantOrganComp, Factory) || //will cause an exception if yaml is configured incorrectly
        implantOrganComp.Category == null ||
        _protoManager.Index<OrganCategoryPrototype>(implantOrganComp.Category).ConnectsTo == null)
            return false;

        var connectsTo = _protoManager.Index<OrganCategoryPrototype>(implantOrganComp.Category).ConnectsTo;

        //are there any organs matching the category in ConnectsTo?
        if (!bodycomponent.Organs.ContainedEntities.Any(p => TryComp<OrganComponent>(p, out var organ) && organ.Category == connectsTo))
        {
            if(connectsTo != null)
                _popupSystem.PopupClient(Loc.GetString("comp-cyberneticimplanter-missingconnectto", ("connectto", connectsTo.Value.ToString())), target, user);
            return false;
        }
        //ready to go! play sfx and start the doafter
        _audio.PlayPredicted(entity.Comp.ImplantBeginSound, entity, user);

        var doAfterEventArgs = new DoAfterArgs(_entityManager, user, entity.Comp.ActivationTime, new CyberneticImplanterDoAfterEvent(), entity, target)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true
        };

        // Server and Client spilt here, dont need the client for the rest of this
        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return false;

        _visualizer.SetData(entity, CyberneticImplanterVisuals.State, CyberneticImplanterState.Implant);


        if (TryComp(entity, out MetaDataComponent? metadata))
            _popupSystem.PopupClient(Loc.GetString("comp-cyberneticimplanter-implantstart", ("implanter", metadata.EntityName)), target, target, PopupType.Medium);

        return true;
    }
}