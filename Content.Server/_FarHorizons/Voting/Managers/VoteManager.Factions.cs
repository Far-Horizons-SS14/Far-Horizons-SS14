using System.Linq;
using Content.Server._FarHorizons.Factions;
using Content.Server.GameTicking;
using Content.Shared._FarHorizons.CCVar;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Voting.Managers;

public sealed partial class VoteManager
{
    [Dependency] private readonly IServerFactionManager _factions = default!;

    private void CreateFactionVote(ICommonSession? initiator)
    {
        var enabledFactions = _factions.ListEnabledFactions();
        var factions = enabledFactions.ToDictionary(faction => faction.Name);
        
        var alone = _playerManager.PlayerCount == 1 && initiator != null;
        var options = new VoteOptions
        {
            Title = Loc.GetString("ui-vote-faction-title"),
            Duration = alone
                ? TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteTimerAlone))
                : TimeSpan.FromSeconds(_cfg.GetCVar(FHCCVars.VoteTimerFaction))
        };

        if (alone)
            options.InitiatorTimeout = TimeSpan.FromSeconds(10);

        foreach (var (k, v) in factions) options.Options.Add((k, v));

        WirePresetVoteInitiator(options, initiator);

        var vote = CreateVote(options);

        vote.OnFinished += (_, args) =>
        {
            FactionPrototype picked;
            if (args.Winner == null)
            {
                picked = (FactionPrototype) _random.Pick(args.Winners);
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("ui-vote-faction-tie"));
            }
            else
            {
                picked = (FactionPrototype) args.Winner;
            }
            _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-map-win"));

            _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Faction vote finished: {picked.Name}");
            var ticker = _entityManager.EntitySysManager.GetEntitySystem<GameTicker>();
            if (!ticker.CanUpdateMap() || !_factions.SetCurrentFaction(picked)) // bc it makes no sense to vote for faction when you cannot set map even
            {
                if (ticker.RoundPreloadTime <= TimeSpan.Zero)
                {
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-faction-notlobby"));
                }
                else
                {
                    var timeString = $"{ticker.RoundPreloadTime.Minutes:0}:{ticker.RoundPreloadTime.Seconds:00}";
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-faction-notlobby-time", ("time", timeString)));
                }
            }
        };
    }
}