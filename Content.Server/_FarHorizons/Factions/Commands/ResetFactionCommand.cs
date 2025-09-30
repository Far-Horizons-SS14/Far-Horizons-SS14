using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.FarHorizons.Factions.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed class ResetFactionCommand : IConsoleCommand
{
    [Dependency] private readonly IServerFactionManager _factions = default!;

    public string Command => "resetfaction";
    public string Description => Loc.GetString("reset-faction-command-description");

    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_factions.SetCurrentFaction(null))
            shell.WriteLine(Loc.GetString("reset-faction-success"));
        else
            shell.WriteLine(Loc.GetString("set-faction-unable-error"));
    }
}
