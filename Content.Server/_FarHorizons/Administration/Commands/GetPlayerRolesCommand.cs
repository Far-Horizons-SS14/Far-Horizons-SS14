using System.Linq;
using Content.Server._NullLink.PlayerData;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._FarHorizons.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class GetPlayerRolesCommand : IConsoleCommand
{
    [Dependency] private readonly INullLinkPlayerManager _nullLinkPlayerManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public string Command => "getplayerroles";
    public string Description => Loc.GetString("get-player-roles-command-description");

    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("get-player-roles-wrong-args-error"));
            return;
        }

        if (!_playerManager.TryGetUserId(args[0], out var netUserId))
        {
            shell.WriteLine(Loc.GetString("get-player-roles-not-found-error"));
            return;
        }

        var roles = _nullLinkPlayerManager.GetUserRoles(netUserId.UserId);
        if (roles == null)
        {
            shell.WriteLine(Loc.GetString("get-player-roles-not-found-error"));
            return;
        }

        shell.WriteLine(Loc.GetString("get-player-roles-success"));
        foreach (var role in roles)
            shell.WriteLine(role.ToString());
    }
}
