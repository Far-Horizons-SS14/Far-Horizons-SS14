using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.FarHorizons.Factions.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed class SetFactionCommand : IConsoleCommand
{
    [Dependency] private readonly IServerFactionManager _factions = default!;

    public string Command => "setfaction";
    public string Description => Loc.GetString("set-faction-command-description");
    public string Help => Loc.GetString("set-faction-command-help-text");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        if (!_factions.TryFindFaction(args[0], out var faction) || faction == null)
        {
            shell.WriteError(Loc.GetString("set-faction-faction-error", ("faction", args[0])));
            return;
        }

        if (!faction.Major)
        {
            shell.WriteError(Loc.GetString("set-faction-major-error"));
            return;
        }

        if (_factions.SetCurrentFaction(faction)){
            shell.WriteLine(Loc.GetString("set-faction-success", ("faction", faction.Name)));
        }
        else
            shell.WriteLine(Loc.GetString("set-faction-unable-error"));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        List<string> options = _factions.ListPlayableFactions().Where(p => p.Major).Select(p => p.ID).ToList();

        return CompletionResult.FromHintOptions(options, "<id, name or alias>");

    }
}
