using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.Voting.Managers;
using Content.Shared.Starlight.CCVar;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Content.Shared._FarHorizons.CCVar; //FH

namespace Content.Server.Starlight.GameTicking;

public sealed partial class RoundEndVoteSystem : EntitySystem
{
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IVoteManager _voteManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private TimeSpan? _voteStartTime;
    //FH Start
    private TimeSpan? _voteFactionStartTime;
    private const float InBetweenVoteTime = 5f;
    private const float MinBeforeStart = 20f;
    //FH End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEndSystemChange);
    }

    public void OnRoundEndSystemChange(RoundEndSystemChangedEvent args)
    {
        if (_playerManager.PlayerCount < _cfg.GetCVar(StarlightCCVars.MinPlayerToVote))
        {
            Logger.Warning($"Not enought players, player count: {_playerManager.PlayerCount}");
            return;
        }
        
        //FH Start
        _voteStartTime = _gameTiming.CurTime + _gameTicker.LobbyDuration - TimeSpan.FromSeconds(_cfg.GetCVar(StarlightCCVars.VotingsDelay) + MinBeforeStart);
        _voteFactionStartTime = _voteStartTime - TimeSpan.FromSeconds(_cfg.GetCVar(FHCCVars.VoteTimerFaction) + InBetweenVoteTime);
        Log.Warning($"Vote will start at {_voteStartTime} & faction vote will start at {_voteFactionStartTime}");
        //FH End

        if (_cfg.GetCVar(StarlightCCVars.ResetPresetAfterRestart))
            _gameTicker.SetGamePreset("Secret");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby || _voteStartTime == null)
            return;

        //FH Start
        if(_voteFactionStartTime != null && 
        _gameTiming.CurTime >= _voteFactionStartTime && 
        _cfg.GetCVar(FHCCVars.RunFactionVoteAfterRestart))
        {
            _voteManager.CreateStandardVote(null, StandardVoteType.Faction);
            _voteFactionStartTime = null;
        }
        //FH End
        if (_gameTiming.CurTime >= _voteStartTime)
        {
            StartRoundEndVotes();
            _voteStartTime = null;
        }
    }

    public void StartRoundEndVotes()
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
            return;

        if (_cfg.GetCVar(StarlightCCVars.RunMapVoteAfterRestart))
            _voteManager.CreateStandardVote(null, StandardVoteType.Map);

        if (_cfg.GetCVar(StarlightCCVars.RunPresetVoteAfterRestart))
            _voteManager.CreateStandardVote(null, StandardVoteType.Preset);
    }
}