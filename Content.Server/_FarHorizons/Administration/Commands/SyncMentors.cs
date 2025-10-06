using System.Linq;
using Content.Server._NullLink.PlayerData;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server._FarHorizons.Administration.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class SyncMentorCommand : IConsoleCommand
{
    [Dependency] private readonly INullLinkPlayerManager _nullLinkPlayerManager = default!;

    public string Command => "syncmentors";
    public string Description => Loc.GetString("sync-mentors-command-description");

    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteLine(Loc.GetString("sync-mentors-wrong-args-error"));
            return;
        }

        _nullLinkPlayerManager.SyncMentors();
        shell.WriteLine(Loc.GetString("sync-mentors-success"));
    }

}