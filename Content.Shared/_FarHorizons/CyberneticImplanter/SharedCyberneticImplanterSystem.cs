using System.Linq;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.CyberneticImplanter;

public abstract partial class SharedCyberneticImplanterSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private EntityManager _entityManager = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedAppearanceSystem _visualizer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberneticImplanterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CyberneticImplanterComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<CyberneticImplanterComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CyberneticImplanterComponent, ExaminedEvent>(OnExamineUnused);
        SubscribeLocalEvent<UsedCyberneticImplanterComponent, ExaminedEvent>(OnExamineUsed);
        SubscribeLocalEvent<CyberneticImplanterModeComponent, ExaminedEvent>(OnExamineMode);
        SubscribeLocalEvent<CyberneticImplanterModeComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
    }

    private void OnMapInit(Entity<CyberneticImplanterComponent> entity, ref MapInitEvent args) => entity.Comp.ImplantedOrganDesc ??= _protoManager.Index<EntityPrototype>(entity.Comp.ImplantedOrgan).Description;

    private void OnExamineUnused(EntityUid entity, CyberneticImplanterComponent component, ExaminedEvent args) //used to show a description of the implant
    {
        if (!args.IsInDetailsRange)
            return;

        if (component.ImplantedOrganDesc != null)
            args.PushMarkup(Loc.GetString("comp-cyberneticimplanter-examine", ("desc", component.ImplantedOrganDesc!)));
    }

    private void OnExamineUsed(EntityUid entity, UsedCyberneticImplanterComponent component, ExaminedEvent args) //used to show what organ was destroyed after an implant
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("comp-usedcyberneticimplanter-examine", ("species", component.Species), ("organ", component.Organ)));
    }

    private void OnExamineMode(EntityUid entity, CyberneticImplanterModeComponent component, ExaminedEvent args) //used to show a description of the selected mode
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gun-cyberneticimplantermode-examine", ("mode", component.Mode
            ? Loc.GetString("comp-cyberneticimplantermode-right")
            : Loc.GetString("comp-cyberneticimplantermode-left"))));
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
        //most of this doesnt need to be done on the client, but doing it this way ensures the doafter isnt laggy on the client
        //if statment straight from hell, does all the checks to verify target is valid
        if (!HasComp<HumanoidProfileComponent>(target) ||
        !TryComp<BodyComponent>(target, out var bodycomponent) ||
        bodycomponent.Organs == null ||
        bodycomponent.Organs.ContainedEntities == null ||
        !_protoManager.Index<EntityPrototype>(entity.Comp.ImplantedOrgan).TryGetComponent<OrganComponent>(out var implantOrganComp, Factory) || //will cause an exception if yaml is configured incorrectly
        implantOrganComp.Category == null ||
        !(_protoManager.Index<OrganCategoryPrototype>(implantOrganComp.Category) is { ConnectsTo: not null } organCategory))
            return false;

        var connectsTo = organCategory.ConnectsTo;

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

    private void OnAltVerb(EntityUid uid, CyberneticImplanterModeComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        AlternativeVerb verb = new()
        {
            Act = () => ToggleMode(uid, component, args.User),
            Text = Loc.GetString("gun-selector-verb", ("mode",
                component.Mode
                    ? Loc.GetString("comp-cyberneticimplantermode-left")
                    : Loc.GetString("comp-cyberneticimplantermode-right"))),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),
        };

        args.Verbs.Add(verb);
    }

    private void ToggleMode(EntityUid uid, CyberneticImplanterModeComponent component, EntityUid user)
    {
        component.Mode = !component.Mode;

        if (TryComp<CyberneticImplanterComponent>(uid, out var comp))
            comp.ImplantedOrgan = component.Mode ? component.RightOrgan : component.LeftOrgan;

        _audio.PlayPredicted(component.ModeSwitchSound, uid, user);
        _popupSystem.PopupClient(Loc.GetString("gun-selected-mode", ("mode", component.Mode 
            ? Loc.GetString("comp-cyberneticimplantermode-right") 
            : Loc.GetString("comp-cyberneticimplantermode-left"))),
            uid, user);
    }
}