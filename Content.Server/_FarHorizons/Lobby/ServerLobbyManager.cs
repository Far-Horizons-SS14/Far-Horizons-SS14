using System.Linq;
using Content.Server._FarHorizons.Factions;
using Content.Server.Preferences.Managers;
using Content.Shared._FarHorizons.Factions;
using Content.Shared._FarHorizons.Lobby;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Lobby;

public sealed partial class ServerLobbyManager : SharedLobbyManager, IServerLobbyManager
{
    [Dependency] private readonly IServerFactionManager _faction = default!;
    [Dependency] private readonly IServerNetManager _netMan = default!;
    [Dependency] private readonly IServerPreferencesManager _prefMan = default!;

    public new void Init()
    {
        base.Init();

        _netMan.RegisterNetMessage<MsgJobPicksUpdated>();
        _netMan.Connected += Connected;
    }

    private void Connected(object? sender, NetChannelArgs args) => SyncCurrentJobPicks(args.Channel);

    private void SyncCurrentJobPicks(INetChannel? target = null)
    {
        var msg = new MsgJobPicksUpdated
        {
            JobPicks = JobPicks
        };

        if (target == null)
            _netMan.ServerSendToAll(msg);
        else
            _netMan.ServerSendMessage(msg, target);
    }

    private void SetJobPicks(Dictionary<ProtoId<FactionJobAssignmentPrototype>, (int, int, int)> jobPicks)
    {
        JobPicks = jobPicks;
        SyncCurrentJobPicks();
    }

    public void RefreshJobPicks(Dictionary<NetUserId, PlayerGameStatus> players)
    {
        Dictionary<ProtoId<FactionJobAssignmentPrototype>, (int, int, int)> result = new();

        var readyPlayers = players.Where(p => p.Value == PlayerGameStatus.ReadyToPlay)
            .Select(p => p.Key).ToList();

        foreach (var pref in readyPlayers.Select(player => _prefMan.GetPreferencesOrNull(player)).Where(p => p != null))
        foreach (var ((faction, job), priority) in pref!.JobPriorities)
        {
            if (priority == JobPriority.Never) continue;

            var assignment = _faction.ListFactionJobs().Where(p => p.Faction == faction && p.Job == job)
                .Select(p => (ProtoId<FactionJobAssignmentPrototype>)p.ID).FirstOrNull();

            if (assignment == null) continue;

            if (!result.ContainsKey(assignment.Value))
                result[assignment.Value] = (0, 0, 0);

            switch (priority)
            {
                case JobPriority.Low:
                    result[assignment.Value] = (result[assignment.Value].Item1 + 1, result[assignment.Value].Item2, result[assignment.Value].Item3);
                    break;
                case JobPriority.Medium:
                    result[assignment.Value] = (result[assignment.Value].Item1, result[assignment.Value].Item2 + 1, result[assignment.Value].Item3);
                    break;
                case JobPriority.High:
                    result[assignment.Value] = (result[assignment.Value].Item1, result[assignment.Value].Item2, result[assignment.Value].Item3 + 1);
                    break;
                default:
                    continue;
            }
        }

        SetJobPicks(result);
    }

    public void PreRoundStarted() => RefreshJobPicks(new Dictionary<NetUserId, PlayerGameStatus>());
    public void RoundStarted() => RefreshJobPicks(new Dictionary<NetUserId, PlayerGameStatus>());
}