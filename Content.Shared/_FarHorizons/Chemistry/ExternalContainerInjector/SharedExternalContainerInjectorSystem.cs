using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Chemistry.ExternalContainerInjector;

/// <summary>
/// System for Injectors that use solutions from inserted vials instead of internal solutions.
/// </summary>
public abstract partial class SharedExternalContainerInjectorSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private ReactiveSystem _reactiveSystem = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainers = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<ExternalContainerInjectorComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        args.Handled = TryDoInject(entity, args.Target.Value, args.User);
    }

    [SubscribeLocalEvent]
    private void AddActivationVerb(Entity<ExternalContainerInjectorComponent> entity,
        ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || entity.Comp.InjectOnly)
            return;

        var (_, component) = entity;
        
        var activationVerb = new ActivationVerb()
        {
            Text = Loc.GetString("injector-toggle-verb-text"),
        };
        args.Verbs.Add(activationVerb);
    }

    public void SetMode(Entity<ExternalContainerInjectorComponent> entity, bool onlyAffectsMobs)
    {
        if (entity.Comp.OnlyAffectsMobs == onlyAffectsMobs)
            return;

        entity.Comp.OnlyAffectsMobs = onlyAffectsMobs;
        Dirty(entity);
    }

    /// <summary>
    /// Gets the solution from the inserted vial, if any.
    /// </summary>
    public bool TryGetVialSolution(Entity<ExternalContainerInjectorComponent> entity, out Solution? solution,
        out Entity<SolutionComponent> solutionEntity)
    {
        solution = null;
        solutionEntity = default;

        if (!_itemSlots.TryGetSlot(entity.Owner, entity.Comp.VialSlotId, out var slot) || !slot.HasItem ||
            slot.Item == null)
            return false;

        if (!_solutionContainers.TryGetSolution(slot.Item.Value, entity.Comp.VialSolutionName, out var vialSolution,
                out var vialSolutionComponent))
            return false;

        if (vialSolutionComponent.Volume == 0)
            return false;

        solution = vialSolutionComponent;
        solutionEntity = vialSolution.GetValueOrDefault();
        return true;
    }

    /// <summary>
    /// Injects a solution from an inserted vial into a target.
    /// </summary>
    public bool TryDoInject(Entity<ExternalContainerInjectorComponent> entity, EntityUid target, EntityUid user)
    {
        var (uid, component) = entity;

        if (!EligibleEntity(target, EntityManager, component))
            return false;

        var hasUseDelay = TryComp<UseDelayComponent>(uid, out var delayComp);
        if (hasUseDelay && _useDelay.IsDelayed((uid, delayComp)))
            return false;

        var selfEvent = new SelfBeforeInjectEvent(user, entity.Owner, target);
        RaiseLocalEvent(user, selfEvent);

        if (selfEvent.Cancelled)
        {
            _popup.PopupClient(
                Loc.GetString(selfEvent.OverrideMessage ?? "injector-component-cannot-inject-message",
                    ("target", Identity.Entity(target, EntityManager))),
                target,
                user);
            return false;
        }

        target = selfEvent.TargetGettingInjected;
        if (!EligibleEntity(target, EntityManager, component))
            return false;

        var targetEvent = new TargetBeforeInjectEvent(user, entity.Owner, target);
        RaiseLocalEvent(target, ref targetEvent);

        if (targetEvent.Cancelled)
        {
            _popup.PopupClient(
                Loc.GetString(targetEvent.OverrideMessage ?? "injector-component-cannot-inject-message",
                    ("target", Identity.Entity(target, EntityManager))),
                target,
                user);
            return false;
        }

        target = targetEvent.TargetGettingInjected;
        if (!EligibleEntity(target, EntityManager, component))
            return false;

        if (!TryGetVialSolution(entity, out var vialSolution, out var vialSolutionEntity))
        {
            _popup.PopupClient(Loc.GetString("injector-component-empty-message", ("injector", entity)), target, user);
            return false;
        }

        if (!_solutionContainers.TryGetInjectableSolution(target, out var targetSoln, out var targetSolution) ||
            targetSolution == null)
        {
            _popup.PopupClient(
                Loc.GetString("injector-component-cannot-inject-message",
                    ("target", Identity.Entity(target, EntityManager))),
                target,
                user);
            return false;
        }

        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetSolution.AvailableVolume);
        if (realTransferAmount <= 0)
        {
            var msg = target == user
                ? "injector-component-target-already-full-message-self"
                : "injector-component-target-already-full-message";
            _popup.PopupClient(
                Loc.GetString(msg, ("target", Identity.Entity(target, EntityManager))),
                target,
                user);
            return false;
        }

        var removedSolution = _solutionContainers.SplitSolution(vialSolutionEntity, realTransferAmount);
        if (!targetSolution.CanAddSolution(removedSolution))
            return true;

        _reactiveSystem.DoEntityReaction(target, removedSolution, ReactionMethod.Injection);
        _solutionContainers.TryAddSolution(targetSoln.Value, removedSolution);
        UpdateHyposprayAppearance(entity);

        if (hasUseDelay)
            _useDelay.TryResetDelay((uid, delayComp!));

        var msgSuccess = target == user
            ? "injector-component-inject-success-message-self"
            : "injector-component-inject-success-message";
        if (selfEvent.OverrideMessage != null)
            msgSuccess = selfEvent.OverrideMessage;
        else if (targetEvent.OverrideMessage != null)
            msgSuccess = targetEvent.OverrideMessage;

        _popup.PopupClient(
            Loc.GetString(msgSuccess, ("amount", removedSolution.Volume), ("target", Identity.Entity(target, EntityManager))),
            target,
            user);
        _audio.PlayPredicted(entity.Comp.InjectSound, entity, user);

        var ev = new TransferDnaEvent { Donor = target, Recipient = uid };
        RaiseLocalEvent(target, ref ev);

        _adminLogger.Add(LogType.ForceFeed,
            $"{ToPrettyString(user):user} injected {ToPrettyString(target):target} with a solution {SharedSolutionContainerSystem.ToPrettyString(removedSolution):removedSolution} using a {ToPrettyString(uid):using}");
        return true;
    }

    protected bool EligibleEntity(EntityUid entity, IEntityManager entMan, ExternalContainerInjectorComponent component)
    {
        var hasInjectableSolution = _solutionContainers.TryGetInjectableSolution(entity, out _, out _);
        return component.OnlyAffectsMobs
            ? hasInjectableSolution &&
              entMan.HasComponent<MobStateComponent>(entity)
            : hasInjectableSolution;
    }

    [SubscribeLocalEvent]
    private void OnSolutionChanged(ref SolutionChangedEvent args)
    {
        var query = EntityQueryEnumerator<ExternalContainerInjectorComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (TryGetVialSolution((uid, component), out _, out var solutionEntity) &&
               solutionEntity.Owner == args.Solution.Owner)
            {
               UpdateHyposprayAppearance((uid, component));
            }
        }
    }

    private void UpdateHyposprayAppearance(Entity<ExternalContainerInjectorComponent> entity)
    {
        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        if (!TryGetVialSolution(entity, out var solution, out _))
        {
            _appearance.SetData(entity, SolutionContainerVisuals.FillFraction, 0f, appearance);
            _appearance.SetData(entity, SolutionContainerVisuals.Color, Color.White, appearance);
            _appearance.SetData(entity, SolutionContainerVisuals.SolutionName, entity.Comp.VialSolutionName, appearance);
            return;
        }

        var vialSolution = solution!;
        _appearance.SetData(entity, SolutionContainerVisuals.FillFraction, vialSolution.FillFraction, appearance);
        _appearance.SetData(entity, SolutionContainerVisuals.Color, vialSolution.GetColor(_prototypeManager), appearance);
        _appearance.SetData(entity, SolutionContainerVisuals.SolutionName, entity.Comp.VialSolutionName, appearance);

        if (vialSolution.GetPrimaryReagentId() is { } reagent)
            _appearance.SetData(entity, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearance);
    }

    [SubscribeLocalEvent]
    private void OnVialInserted(Entity<ExternalContainerInjectorComponent> entity, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == entity.Comp.VialSlotId)
            UpdateHyposprayAppearance(entity);
    }

    [SubscribeLocalEvent]
    private void OnVialRemoved(Entity<ExternalContainerInjectorComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == entity.Comp.VialSlotId)
            UpdateHyposprayAppearance(entity);
    }
}
