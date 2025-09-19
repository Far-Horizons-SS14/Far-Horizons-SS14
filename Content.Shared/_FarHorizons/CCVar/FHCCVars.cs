using Robust.Shared.Configuration;

namespace Content.Shared._FarHorizons.CCVar;

[CVarDefs]
public sealed class FHCCVars
{
    
    public static readonly CVarDef<string> ServerName =
        CVarDef.Create("lobby.server_name", "Far Horizons", CVar.SERVER | CVar.REPLICATED);

}