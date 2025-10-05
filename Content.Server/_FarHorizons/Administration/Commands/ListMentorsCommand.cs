using System.Linq;
using Content.Server._NullLink.PlayerData;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.Database.Migrations.Sqlite;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server._FarHorizons.Administration.Commands;

[AdminCommand(AdminFlags.Permissions)]
public sealed class ListMentorsCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly INullLinkPlayerManager _nullLinkPlayerManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    public string Command => "listmentors";
    public string Description => Loc.GetString("list-mentors-command-description");

    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteLine(Loc.GetString("list-mentors-wrong-args-error"));
            return;
        }

        ListMentorsDB(shell);
    }

    private async void ListMentorsDB(IConsoleShell shell)
    {
        try
        {
            var mentorsIds = await _dbManager.GetMentorsAsync();
            var mentors = await _dbManager.GetPlayerRecordsByUserIds(mentorsIds);
            
            shell.WriteLine(Loc.GetString("list-mentors-success"));
            foreach (var mentor in mentors)
            {
                shell.WriteLine(mentor.LastSeenUserName);
            }
        }
        catch (Exception e)
        {
            shell.WriteError(e.Message);
        }
    }
}