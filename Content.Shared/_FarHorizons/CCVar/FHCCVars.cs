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
        CVarDef.Create("factions.votable_factions", "FactionNT,FactionSyndicate", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the duration of the faction vote timer.
    /// </summary>
    public static readonly CVarDef<int>
        VoteTimerFaction = CVarDef.Create("vote.timerfaction", 90, CVar.SERVERONLY);

    public static readonly CVarDef<string> LimbTargettingStyle =
        CVarDef.Create("ui.limb_targetting_style", "LimbTargetHuman",
            CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> LimbTargettingMatchSpecies =
        CVarDef.Create("ui.limb_targetting_match_species", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> HostileTelegraphsColor = 
        CVarDef.Create("accessibility.hostile_telegraphs_color", "#FF0000FF", CVar.CLIENTONLY | CVar.ARCHIVE);
    
    public static readonly CVarDef<string> UtilityTelegraphsColor = 
        CVarDef.Create("accessibility.utility_telegraphs_color", "#FFA500FF", CVar.CLIENTONLY | CVar.ARCHIVE);
    
    public static readonly CVarDef<bool> ChatShowFactionPrefix =
        CVarDef.Create("chat.show_faction_prefix", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> MigrateDoors =
        CVarDef.Create("migration.doors", false, CVar.SERVERONLY);

    /// <summary>
    /// Exponential decay constant used to shape raw occlusion into the final muffle amount.
    /// </summary>
    public static readonly CVarDef<float> AudioOcclusionMuffleDecay =
        CVarDef.Create("audio.occlusion_muffle_decay", 0.3f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Maximum occlusion value that can be applied to a sound.
    /// </summary>
    public static readonly CVarDef<float> AudioOcclusionMax =
        CVarDef.Create("audio.occlusion_max", 10f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Max change in shaped occlusion allowed per second, to smooth out spatial pops.
    /// </summary>
    public static readonly CVarDef<float> AudioOcclusionSmoothingRate =
        CVarDef.Create("audio.occlusion_smoothing_rate", 12f, CVar.SERVER | CVar.REPLICATED);
    
    public static readonly CVarDef<bool> RenderStarSystem =
        CVarDef.Create("render.star_system", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}