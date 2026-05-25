using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Mobs;

public abstract partial class SharedActiveCritSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeActions();

        SubscribeLocalEvent<ActiveCritComponent, MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<ActiveCritComponent, RefreshMovementSpeedModifiersEvent>(OnMovementModifierRefresh);
        SubscribeLocalEvent<ActiveCritComponent, GetStandUpTimeEvent>(SetStandUpTime);
        SubscribeLocalEvent<ActiveCritComponent, TryStandDoAfterEvent>(OnAfterStandup,
            after: [typeof(SharedStunSystem)]);
        SubscribeLocalEvent<ActiveCritComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<ActiveCritComponent, ThrowAttemptEvent>(OnThrowAttempt);
        SubscribeLocalEvent<ActiveCritComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<ActiveCritComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<ActiveCritComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<ActiveCritComponent, ConsciousAttemptEvent>(OnConsciousCheck);
        SubscribeLocalEvent<ActiveCritComponent, UpdateCanMoveEvent>(OnCanMoveCheck);
        SubscribeLocalEvent<ActiveCritComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<ActiveCritComponent, InRangeOverrideEvent>(OnInRangeCheck);
    }

    private void OnInRangeCheck(Entity<ActiveCritComponent> ent, ref InRangeOverrideEvent args)
    {
        if (!_mobState.IsCritical(ent.Owner) || args.Handled || args.Action) return;

        args.InRange = false;
        args.Handled = true;
    }

    private void OnSpeakAttempt(Entity<ActiveCritComponent> ent, ref SpeakAttemptEvent args)
    {
        if (!_mobState.IsCritical(ent.Owner) || args.Cancelled) return;

        if (ent.Comp.Blackout)
            args.Cancel();
    }

    private void OnCanMoveCheck(Entity<ActiveCritComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (!_mobState.IsCritical(ent.Owner) || args.Cancelled) return;

        if (ent.Comp.Blackout)
            args.Cancel();
    }

    private void OnConsciousCheck(Entity<ActiveCritComponent> ent, ref ConsciousAttemptEvent args)
    {
        if (!_mobState.IsCritical(ent.Owner) || args.Cancelled) return;

        args.Cancelled = ent.Comp.Blackout;
    }

    private void OnUseAttempt(Entity<ActiveCritComponent> ent, ref UseAttemptEvent args)
    {
        if (!_mobState.IsCritical(ent.Owner) || args.Cancelled || !_timing.IsFirstTimePredicted) return;

        if (HasComp<KnockedDownComponent>(ent.Owner))
        {
            args.Cancel();
            return;
        }

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        if (!random.Prob(ent.Comp.FallOnUseChance)) return;
        _stun.TryKnockdown(ent.Owner, ent.Comp.CrawlDuration, true, false, true, true);
        _damage.TryChangeDamage(ent.Owner, ent.Comp.DamageOnFall, true, false);
        args.Cancel();
    }

    private void OnInteractAttempt(Entity<ActiveCritComponent> ent, ref InteractionAttemptEvent args)
    {
        if (!_mobState.IsCritical(ent.Owner) || args.Cancelled) return;

        if (HasComp<KnockedDownComponent>(ent.Owner))
            args.Cancelled = true;
    }

    private void OnPickupAttempt(Entity<ActiveCritComponent> ent, ref PickupAttemptEvent args)
    {
        if (!_mobState.IsCritical(ent.Owner) || args.Cancelled || !HasComp<KnockedDownComponent>(ent.Owner))
            return;

        args.Cancel();
        _popup.PopupClient(Loc.GetString(ent.Comp.CantUseHandsMessage), ent.Owner);
    }

    private void OnThrowAttempt(Entity<ActiveCritComponent> ent, ref ThrowAttemptEvent args)
    {
        if (ent.Comp.RestrictCombat && _mobState.IsCritical(ent.Owner))
            args.Cancel();
    }

    private void OnAttackAttempt(Entity<ActiveCritComponent> ent, ref AttackAttemptEvent args)
    {
        if (ent.Comp.RestrictCombat && _mobState.IsCritical(ent.Owner))
            args.Cancel();
    }

    private void OnAfterStandup(Entity<ActiveCritComponent> ent, ref TryStandDoAfterEvent args)
    {
        if (!_mobState.IsCritical(ent.Owner) || args.Cancelled) return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        if (!random.Prob(ent.Comp.FallAfterStandingChance)) return;

        _stun.TryKnockdown(ent.Owner, ent.Comp.CrawlDuration, true, false, true, true);
        _damage.TryChangeDamage(ent.Owner, ent.Comp.DamageOnFall, true, false);
        _popup.PopupPredicted(
            Loc.GetString(ent.Comp.FailedStandUpMessage, ("target", Identity.Entity(ent.Owner, EntityManager))),
            ent.Owner, ent.Owner, PopupType.MediumCaution);
    }

    private void SetStandUpTime(Entity<ActiveCritComponent> ent, ref GetStandUpTimeEvent args)
    {
        if (_mobState.IsCritical(ent.Owner))
            args.DoAfterTime *= ent.Comp.StandUpDoafterModifier;
    }

    private void OnMovementModifierRefresh(Entity<ActiveCritComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (_mobState.IsCritical(ent.Owner))
            args.ModifySpeed(ent.Comp.WalkSpeedModifier, ent.Comp.SprintSpeedModifier);
    }

    private void OnStateChanged(Entity<ActiveCritComponent> ent, ref MobStateChangedEvent args)
    {
        if (!_timing.IsFirstTimePredicted) return;

        if (args.OldMobState == MobState.ActiveCritical && args.NewMobState != MobState.ActiveCritical)
            CleanupActiveCrit(ent.AsNullable());
        else if (args.OldMobState != MobState.ActiveCritical && args.NewMobState == MobState.ActiveCritical)
            SetupActiveCrit(ent.AsNullable());
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted) return;

        var query = EntityQueryEnumerator<ActiveCritComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.BlackoutToggleAt == null || comp.BlackoutToggleAt > _timing.CurTime) continue;

            if (comp.Blackout)
                ExitBlackout((uid, comp));
            else
                EnterBlackout((uid, comp));
        }
    }

    public void SetupActiveCrit(Entity<ActiveCritComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp) || !_mobState.IsCritical(ent))
            return;

        _stun.TryKnockdown(ent.Owner, ent.Comp.CrawlDuration, true, false, true, true);
        _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);
        EnterBlackout(ent);
        _combat.SetInCombatMode(ent.Owner, false);

        if (ent.Comp.AdjustTemporaryEyeDamage <= 0) 
            return;
        
        _blindable.AdjustEyeDamage(ent.Owner, ent.Comp.AdjustTemporaryEyeDamage);

    }

    public void CleanupActiveCrit(Entity<ActiveCritComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ExitBlackout(ent);
        ent.Comp.BlackoutToggleAt = null;
        _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);

        if (ent.Comp.AdjustTemporaryEyeDamage <= 0) 
            return;
        
        _blindable.AdjustEyeDamage(ent.Owner, -ent.Comp.AdjustTemporaryEyeDamage);
    }

    public void EnterBlackout(Entity<ActiveCritComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp) || !_mobState.IsCritical(ent))
            return;
        
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        ent.Comp.Blackout = true;
        ent.Comp.BlackoutToggleAt = _timing.CurTime +
            TimeSpan.FromSeconds(random.Next(ent.Comp.MinBlackoutSeconds, ent.Comp.MaxBlackoutSeconds));
        _stun.TryKnockdown(ent.Owner, ent.Comp.CrawlDuration, true, false, true, true);
        _actionBlocker.UpdateCanMove(ent.Owner);
    }

    public void ExitBlackout(Entity<ActiveCritComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp) || !_mobState.IsCritical(ent))
            return;
        
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        ent.Comp.Blackout = false;
        ent.Comp.BlackoutToggleAt = _timing.CurTime +
            TimeSpan.FromSeconds(random.Next(ent.Comp.MinAwakeSeconds, ent.Comp.MaxAwakeSeconds));
        _actionBlocker.UpdateCanMove(ent.Owner);
    }

    public bool IsBlackout(Entity<ActiveCritComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !_mobState.IsCritical(ent))
            return false;

        return ent.Comp.Blackout;
    }

    public bool ForceWhisper(Entity<ActiveCritComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !_mobState.IsCritical(ent))
            return false;

        if (ent.Comp.WhisperChance >= 1)
            return true;
        if (ent.Comp.WhisperChance <= 0)
            return false;
        
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        if (random.Prob(ent.Comp.WhisperChance))
            return true;

        return false;
    }
}