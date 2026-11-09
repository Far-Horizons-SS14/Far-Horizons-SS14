using Robust.Shared.Configuration;

namespace Content.Shared._FarHorizons.CCVar;

public sealed partial class FHCCVars
{
    public static readonly CVarDef<bool> RunFactionVoteAfterRestart = 
        CVarDef.Create("vote.run_faction_vote_after_restart", true);
        
    /// <summary>
    ///     List of factions enabled for vote.
    /// </summary>
    public static readonly CVarDef<string> VotableFactions =
        CVarDef.Create("factions.votable_factions", "FactionNT,FactionSyndicate", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the duration of the faction vote timer.
    /// </summary>
    public static readonly CVarDef<int> VoteTimerFaction = 
        CVarDef.Create("vote.timerfaction", 30, CVar.SERVERONLY);
}