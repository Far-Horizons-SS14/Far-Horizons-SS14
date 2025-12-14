using Robust.Shared.Configuration;

namespace Content.Server._FarHorizons.DiscordLink;

[CVarDefs]
public sealed class DiscordLinkCCVars
{
    public static readonly CVarDef<string> BotToken =
        CVarDef.Create("discordlink.bot_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
    
    public static readonly CVarDef<string> ClientId =
        CVarDef.Create("discordlink.client_id", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
    
    public static readonly CVarDef<string> ClientSecret =
        CVarDef.Create("discordlink.client_secret", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
    
    public static readonly CVarDef<string> RedirectUrl =
        CVarDef.Create("discordlink.redirect_url", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
    
    public static readonly CVarDef<string> GuildId =
        CVarDef.Create("discordlink.guild_id", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
