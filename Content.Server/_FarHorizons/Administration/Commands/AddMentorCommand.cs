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

[AdminCommand(AdminFlags.Permissions)]
public sealed class AddMentorCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly INullLinkPlayerManager _nullLinkPlayerManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    public string Command => "addmentor";
    public string Description => Loc.GetString("add-mentor-command-description");

    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("add-mentor-wrong-args-error"));
            return;
        }

        if (_playerManager.TryGetUserId(args[0], out var netUserId))
        {
            _nullLinkPlayerManager.AddUserRole(netUserId.UserId, 1407078878786359348);  // TODO: take it from proto
        }

        AddMentorDB(shell, args[0]);
    }

    private async void AddMentorDB(IConsoleShell shell, string username)
    {
        try
        {
            var user = await _dbManager.GetPlayerRecordByUserName(username);
            if (user == null)
            {
                shell.WriteLine(Loc.GetString("add-mentor-not-found-error"));
                return;
            }
            await _dbManager.AddMentorAsync(user.UserId);
            shell.WriteLine(Loc.GetString("add-mentor-success"));
        }
        catch (Exception e)
        {
            shell.WriteError(e.Message);
        }
    }
}
