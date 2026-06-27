using Content.Shared._FarHorizons.LimbDamage.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.CCVar;

[CVarDefs]
public sealed partial class FHCCVars
{
    
    public static readonly CVarDef<string> ServerName =
        CVarDef.Create("lobby.server_name", "Far Horizons", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     List of factions enabled for vote.
    /// </summary>
    public static readonly CVarDef<string> VotableFactions =
        CVarDef.Create("factions.votable_factions", "FactionNT", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the duration of the faction vote timer.
    /// </summary>
    public static readonly CVarDef<int>
        VoteTimerFaction = CVarDef.Create("vote.timerfaction", 90, CVar.SERVERONLY);

    public static readonly CVarDef<ProtoId<LimbTargettingPrototype>> LimbTargettingStyle =
        CVarDef.Create("ui.limb_targetting_style", (ProtoId<LimbTargettingPrototype>)"LimbTargetHuman",
            CVar.CLIENTONLY);

    public static readonly CVarDef<bool> LimbTargettingMatchSpecies =
        CVarDef.Create("ui.limb_targetting_match_species", false, CVar.CLIENTONLY);


}