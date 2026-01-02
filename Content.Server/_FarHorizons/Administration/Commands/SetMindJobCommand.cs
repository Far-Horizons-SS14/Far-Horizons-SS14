using Content.Server.Administration;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SetMindJobCommand : LocalizedEntityCommands
{
    public override string Command => "setmindjob";
    public override string Description => "Sets job to the player's mind.";
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var netUserId = new NetUserId();

        if (Guid.TryParse(args[0], out Guid guid) && _playerManager.ValidSessionId(new NetUserId(guid)))
            netUserId = new NetUserId(guid);

        else if (!_playerManager.TryGetUserId(args[0], out netUserId))
        {
            shell.WriteLine("User not found.");
            return;
        }
        
        EntityUid mindId;
        if(!_mind.TryGetMind(netUserId, out var outMindId, out var mind))
            (mindId, mind) = _mind.CreateMind(netUserId);
        else
            mindId = outMindId.Value;
        
        if (!_prototypeManager.HasIndex<JobPrototype>(args[1])) 
            shell.WriteLine("Unable to find job prototype.");

        _roles.MindAddJobRole(mindId, mind, true, args[1]);
    
    }
}