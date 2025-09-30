using System.Globalization;
using Content.Server.Chat.Managers;
using Content.Server.FarHorizons.Factions;
using Content.Shared.FarHorizons.Factions;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles.Jobs;

/// <summary>
///     Handles the job data on mind entities.
/// </summary>
public sealed class JobSystem : SharedJobSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly IServerFactionManager _factions = default!; // Far Horizons

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAddedEvent);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleRemovedEvent);
    }

    private void OnRoleAddedEvent(RoleAddedEvent args)
    {
        MindOnDoGreeting(args.MindId, args.Mind, args);

        if (args.RoleTypeUpdate)
            _roles.RoleUpdateMessage(args.Mind);
    }

    private void OnRoleRemovedEvent(RoleRemovedEvent args)
    {
        if (args.RoleTypeUpdate)
            _roles.RoleUpdateMessage(args.Mind);
    }

    private void MindOnDoGreeting(EntityUid mindId, MindComponent component, RoleAddedEvent args)
    {
        if (args.Silent)
            return;

        if (!_player.TryGetSessionById(component.UserId, out var session))
            return;

        if (!MindTryGetJob(mindId, out var prototype))
            return;

        _chat.DispatchServerMessage(session, Loc.GetString("job-greet-introduce-job-name",
            ("jobName", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(MindTryGetJobName(mindId))))); // Far Horizons faction name override

        if (prototype.RequireAdminNotify)
            _chat.DispatchServerMessage(session, Loc.GetString("job-greet-important-disconnect-admin-notify"));

        // Far Horizons faction name override
        _chat.DispatchServerMessage(session, Loc.GetString("job-greet-supervisors-warning", ("jobName", MindTryGetJobName(mindId)), ("supervisors", Loc.GetString(prototype.Supervisors))));
        
        // Starlight
        _chat.DispatchServerMessage(session, Loc.GetString("job-greet-information-rules", ("jobRules", Loc.GetString(prototype.JobRules))));
    }

    // Far Horizons
    public void MindAddJob(EntityUid mindId, string jobPrototypeId, ProtoId<FactionPrototype>? factionPrototypeId = null)
    {
        if (MindHasJobWithId(mindId, jobPrototypeId))
            return;

        _roles.MindAddJobRole(mindId, null, false, jobPrototypeId, factionPrototypeId); // Far Horizons
    }
}
