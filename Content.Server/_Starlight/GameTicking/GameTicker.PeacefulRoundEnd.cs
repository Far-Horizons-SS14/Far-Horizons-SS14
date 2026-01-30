using Content.Server._FarHorizons.DiscordLink;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared._FarHorizons.DiscordLink;
using Content.Shared.Chemistry.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.GameTicking;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Starlight.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Starlight.GameTicking;

public sealed class PeacefulRoundEndSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IDiscordLinkManager _discordLink = default!;  // Far Horizons
    [Dependency] private readonly SharedJobSystem _jobs = default!;  // Far Horizons
    [Dependency] private readonly MindSystem _mindSystem = default!;  // Far Horizons

    private bool _isEnabled = false;
    private bool _roundedEnded = false;
    private List<ProtoId<JobPrototype>> _allowedJobs = new();


    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(StarlightCCVars.PeacefulRoundEnd, v => _isEnabled = v, true);

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnded);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
        SubscribeLocalEvent<GotRehydratedEvent>(OnRehydrateEvent);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
        
        // Far Horizons
        if (_proto.TryIndex(typeof(DepartmentPrototype), $"CentralCommand", out var centCommDept))
        {
            var cc = (DepartmentPrototype)centCommDept;
            _allowedJobs.AddRange(cc.Roles);
        }
    }

    // Far Horizons
    private bool IsRoleAllowed(EntityUid target)
    {
        if (_mindSystem.TryGetMind(target, out var mindId, out var mind) && _jobs.MindTryGetJob(mindId, out var prototype))
            return _allowedJobs.Contains(prototype);

        return false;
    }

    private void SpreadPeace(EntityUid target)
    {
        if (!_isEnabled || !_roundedEnded) return;
        if (_discordLink.HasPermission(target, AdditionalPermissionsTypes.PeacefulBypass) || IsRoleAllowed(target)) return;  // Far Horizons
        EnsureComp<PacifiedComponent>(target);
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
        => SpreadPeace(ev.Mob);

    private void OnRehydrateEvent(ref GotRehydratedEvent ev)
        => SpreadPeace(ev.Target);

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
        => _roundedEnded = false;

    private void OnRoundEnded(RoundEndTextAppendEvent ev)
    {
        _roundedEnded = true;

        var mobMoverQuery = EntityQueryEnumerator<MobMoverComponent>();
        while (mobMoverQuery.MoveNext(out var uid, out _))
            SpreadPeace(uid);

        var mechQuery = EntityQueryEnumerator<MechComponent>();
        while (mechQuery.MoveNext(out var uid, out _))
            SpreadPeace(uid);
    }
}
