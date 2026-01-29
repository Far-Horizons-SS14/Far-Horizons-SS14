using Robust.Shared.Configuration;

namespace Content.Shared._FarHorizons.CCVar;

[CVarDefs]
public sealed class FHCCVars
{
    
    public static readonly CVarDef<string> ServerName =
        CVarDef.Create("lobby.server_name", "Far Horizons", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     List of factions enabled for vote.
    /// </summary>
    public static readonly CVarDef<string> EnabledFactions =
        CVarDef.Create("factions.enabled_factions", "FactionNT", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the duration of the faction vote timer.
    /// </summary>
    public static readonly CVarDef<int>
        VoteTimerFaction = CVarDef.Create("vote.timerfaction", 90, CVar.SERVERONLY);
}