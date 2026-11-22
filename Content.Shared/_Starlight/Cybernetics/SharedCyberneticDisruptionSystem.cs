using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Database;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared._Starlight.Cybernetics.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Random;
using Content.Shared.Body;
using Content.Shared.Random.Helpers;

namespace Content.Shared._Starlight.Cybernetics;

public abstract partial class SharedCyberneticDisruptionSystem : EntitySystem
{
    public static readonly EntProtoId DisruptionId = "StatusEffectCyberneticDisruption";

    [Dependency] protected IGameTiming GameTiming = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected AlertsSystem Alerts = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private BodySystem _body = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CyberneticDisruptionComponent, ComponentStartup>(UpdateCybernetics);
        SubscribeLocalEvent<CyberneticDisruptionComponent, ComponentShutdown>(OnCyberneticDisruptionShutdown);

        // New Status Effect subscriptions
        SubscribeLocalEvent<CyberneticDisruptionStatusEffectComponent, StatusEffectAppliedEvent>(OnCyberneticDisruptionStatusApplied);
        SubscribeLocalEvent<CyberneticDisruptionStatusEffectComponent, StatusEffectRemovedEvent>(OnCyberneticDisruptionStatusRemoved);
        SubscribeLocalEvent<CyberneticDisruptionStatusEffectComponent, StatusEffectRelayedEvent<CyberneticDisruptionEndAttemptEvent>>(OnCyberneticDisruptionEndAttempt);

        SubscribeLocalEvent<CyberneticDisruptionOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnCyberneticDisruptionShutdown(Entity<CyberneticDisruptionComponent> ent, ref ComponentShutdown args) => UpdateCybernetics(ent, ent.Comp, args);

    private void UpdateCybernetics(EntityUid uid, CyberneticDisruptionComponent component, EntityEventArgs args)
    {
        if(!TryComp<BodyComponent>(uid, out var body) || body.Organs == null) return;

        var ev = new CyberneticDisruptionEvent(uid);
        _body.RelayEvent((uid, body), ref ev);
    }

    public bool TryAddCyberneticDisruptionDuration(EntityUid uid, TimeSpan duration, bool refreshDuration = false)
    {
        if (refreshDuration && !_status.TrySetStatusEffectDuration(uid, DisruptionId, duration))
            return false;

        if (!refreshDuration && !_status.TryAddStatusEffectDuration(uid, DisruptionId, duration))
            return false;

        OnCyberneticDisruptionSuccessful(uid, duration);
        return true;
    }

    public bool TryUpdateCyberneticDisruptionDuration(EntityUid uid, TimeSpan? duration)
    {
        if (!_status.TryUpdateStatusEffectDuration(uid, DisruptionId, duration))
            return false;

        OnCyberneticDisruptionSuccessful(uid, duration);
        return true;
    }

    private void OnCyberneticDisruptionSuccessful(EntityUid uid, TimeSpan? duration)
    {
        if(!TryComp<BodyComponent>(uid, out var body) || body.Organs == null) return;
        var ev = new CyberneticDisruptionEvent(uid);

        _body.RelayEvent((uid, body), ref ev);

        var timeForLogs = duration.HasValue
            ? duration.Value.Seconds.ToString()
            : "Infinite";
        _adminLogger.Add(LogType.EntityEffect, LogImpact.Low, $"{ToPrettyString(uid):user} disrupted for {timeForLogs} seconds");
    }

    public bool TryRemoveCyberneticDisruption(Entity<CyberneticDisruptionComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return true;

        var ev = new CyberneticDisruptionEndAttemptEvent();
        RaiseLocalEvent(entity, ref ev);

        return !ev.Cancelled && RemComp<CyberneticDisruptionComponent>(entity);
    }

    private void OnCyberneticDisruptionStatusApplied(Entity<CyberneticDisruptionStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        if (GameTiming.ApplyingState)
            return;

        EnsureComp<CyberneticDisruptionComponent>(args.Target);
    }

    private void OnCyberneticDisruptionStatusRemoved(Entity<CyberneticDisruptionStatusEffectComponent> entity, ref StatusEffectRemovedEvent args) => TryRemoveCyberneticDisruption(args.Target);

    private void OnCyberneticDisruptionEndAttempt(Entity<CyberneticDisruptionStatusEffectComponent> entity, ref StatusEffectRelayedEvent<CyberneticDisruptionEndAttemptEvent> args)
    {
        if (args.Args.Cancelled)
            return;

        var ev = args.Args;
        ev.Cancelled = true;
        args.Args = ev;
    }

    private void OnProjectileHit(EntityUid uid, CyberneticDisruptionOnCollideComponent component, ref ProjectileHitEvent args)
    => OnCollide(uid, component, args.Target);

    private void OnCollide(EntityUid uid, CyberneticDisruptionOnCollideComponent component, EntityUid target)
    {
        var rand = SharedRandomExtensions.PredictedRandom(GameTiming, GetNetEntity(uid), GetNetEntity(target)); // FarHorizons
        if(rand.NextFloat() <= component.DisableChance) // FarHorizons
            TryAddCyberneticDisruptionDuration(target, component.Duration);
    }
}
