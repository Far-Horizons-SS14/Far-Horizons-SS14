using System.Linq;
using Content.Server._NullLink.PlayerData;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._FarHorizons.Administration.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class AddPlayerRolesCommand : IConsoleCommand
{
    [Dependency] private readonly INullLinkPlayerManager _nullLinkPlayerManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public string Command => "addplayerrole";
    public string Description => Loc.GetString("add-player-role-command-description");

    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("add-player-role-wrong-args-error"));
            return;
        }

        if (!_playerManager.TryGetUserId(args[0], out var netUserId))
        {
            shell.WriteLine(Loc.GetString("add-player-role-not-found-error"));
            return;
        }

        ulong roleId;
        try
        {
            roleId = ulong.Parse(args[1]);
        }
        catch (FormatException)
        {
            shell.WriteLine(Loc.GetString("add-player-role-wrong-args-error"));
            throw;
        }

        _nullLinkPlayerManager.AddUserRole(netUserId.UserId, roleId);

        shell.WriteLine(Loc.GetString("add-player-role-success"));
    }
}