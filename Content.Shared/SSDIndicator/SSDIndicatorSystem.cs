using Content.Shared.Bed.Sleep; // Starlight-edit
using Content.Shared.CCVar;
using Content.Shared.Movement.Events; // Starlight-edit
using Content.Shared.StatusEffectNew;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SSDIndicator;

/// <summary>
///     Handle changing player SSD indicator status
/// </summary>
public sealed class SSDIndicatorSystem : EntitySystem
{
    public static readonly EntProtoId StatusEffectSSDSleeping = "StatusEffectSSDSleeping";

    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SleepingSystem _sleep = default!; // Starlight

    private bool _icSsdSleep;
    private float _icSsdSleepTime;

    public override void Initialize()
    {
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<SSDIndicatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SSDIndicatorComponent, MoveInputEvent>(OnMoveInput); // Starlight
        SubscribeLocalEvent<SSDIndicatorComponent, WakeActionEvent>(OnWakeAction); // Starlight

        _cfg.OnValueChanged(CCVars.ICSSDSleep, obj => _icSsdSleep = obj, true);
        _cfg.OnValueChanged(CCVars.ICSSDSleepTime, obj => _icSsdSleepTime = obj, true);
    }

    private void OnPlayerAttached(EntityUid uid, SSDIndicatorComponent component, PlayerAttachedEvent args)
    {
        TryRemoveSSD(uid, component); // Starlight
    }

    private void OnPlayerDetached(EntityUid uid, SSDIndicatorComponent component, PlayerDetachedEvent args)
    {
        TrySSD(uid, component); // Starlight
    }

    // Starlight start
    private void OnMoveInput(EntityUid uid, SSDIndicatorComponent comp, MoveInputEvent args)
    {
        TryRemoveSSD(uid, comp);
    }

    private void OnWakeAction(EntityUid uid, SSDIndicatorComponent comp, WakeActionEvent args)
    {
        TryRemoveSSD(uid, comp);
    }
    // Starlight end (for now :P)

    // Prevents mapped mobs to go to sleep immediately
    private void OnMapInit(EntityUid uid, SSDIndicatorComponent component, MapInitEvent args)
    {
        if (!_icSsdSleep || !component.IsSSD)
            return;

        component.FallAsleepTime = _timing.CurTime + TimeSpan.FromSeconds(_icSsdSleepTime);
        component.NextUpdate = _timing.CurTime + component.UpdateInterval;
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_icSsdSleep)
            return;

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SSDIndicatorComponent>();

        while (query.MoveNext(out var uid, out var ssd))
        {
            // Forces the entity to sleep when the time has come
            if (!ssd.IsSSD
                || ssd.NextUpdate > curTime
                || ssd.FallAsleepTime > curTime
                || TerminatingOrDeleted(uid))
                continue;

            _statusEffects.TryUpdateStatusEffectDuration(uid, StatusEffectSSDSleeping);
            ssd.NextUpdate += ssd.UpdateInterval;
            Dirty(uid, ssd);
        }
    }

     #region Starlight

    /// <summary>
    /// STARLIGHT
    /// Attempts to set the entity as SSD.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="comp"></param>
    /// <returns>True if succesful</returns>
    public bool TrySSD(EntityUid uid, SSDIndicatorComponent? comp)
    {
        if (comp == null)
            return false;

        if (comp.IsSSD)
            return false;

        comp.IsSSD = true;

        if (_icSsdSleep)
            comp.FallAsleepTime = _timing.CurTime + TimeSpan.FromSeconds(_icSsdSleepTime);

        _sleep.TrySleeping(uid);

        Dirty(uid, comp);
        return true;
    }

    /// <summary>
    /// STARLIGHT
    /// Attempts to remove the SSD condition from the entity.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="comp"></param>
    /// <returns>True if succesful</returns>
    public bool TryRemoveSSD(EntityUid uid, SSDIndicatorComponent? comp)
    {
        bool success = false;

        if (comp == null)
            return success;

        comp.IsSSD = false;

        if (_icSsdSleep)
        {
            comp.FallAsleepTime = TimeSpan.Zero;
            _statusEffects.TryRemoveStatusEffect(uid, StatusEffectSSDSleeping);
            success = true;
        }

        Dirty(uid, comp);
        return success;
    }
    
    #endregion
}
