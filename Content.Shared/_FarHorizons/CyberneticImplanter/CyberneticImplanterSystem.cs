using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
// using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
// using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.Commands.Values;

namespace Content.Shared._FarHorizons.CyberneticImplanter;

public sealed class CyberneticImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    // [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

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
        entity.Comp.ImplantedOrgan == null ||
        !_protoManager.Index<EntityPrototype>(entity.Comp.ImplantedOrgan).TryGetComponent<OrganComponent>(out var ImplantOrganComponent, Factory) ||
        ImplantOrganComponent.Category == null ||
        _protoManager.Index<OrganCategoryPrototype>(ImplantOrganComponent.Category).ConnectsTo == null)
            return false;

        var ConnectsTo = _protoManager.Index<OrganCategoryPrototype>(ImplantOrganComponent.Category).ConnectsTo;
        var ValidConnection = false;

        //does this body have a valid organ to connect the implant to? ex: trying to implant hands on bodys that dont have arms
        foreach (var organ in bodycomponent.Organs.ContainedEntities)
        {
            if (!TryComp<OrganComponent>(organ, out var OrganComponent))
                continue;

            if (OrganComponent.Category == ConnectsTo)
                ValidConnection = true; //we dont need to know what organ it is, will recheck upon doafter completion
        }

        //this check will be redone once doafter is complete (things might have changed in the time to complete the doafter)
        if (!ValidConnection)
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

        // Server and Client Spilt here, dont need the client for the rest of this
        _doAfter.TryStartDoAfter(doAfterEventArgs);

        return true;
    }
}