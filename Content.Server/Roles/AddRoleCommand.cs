using Content.Server.Administration;
using Content.Server.Roles.Jobs;
using Content.Shared.Administration;
using Content.Shared.FarHorizons.Factions;
using Content.Shared.Players;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddRoleCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly JobSystem _jobSystem = default!;

        public override string Command => "addrole";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            // Far Horizons edit - select faction when adding role
            if (args.Length < 2)
            {
                shell.WriteLine(Loc.GetString($"shell-need-minimum-arguments",
                    ("minimum", 2)));
                return;
            }

            if (!_playerManager.TryGetPlayerDataByUsername(args[0], out var data))
            {
                shell.WriteLine(Loc.GetString($"cmd-addrole-mind-not-found"));
                return;
            }

            var mind = data.ContentData()?.Mind;
            if (mind == null)
            {
                shell.WriteLine(Loc.GetString($"cmd-addrole-mind-not-found"));
                return;
            }

            if (!_prototypeManager.TryIndex<JobPrototype>(args[1], out var jobPrototype))
            {
                shell.WriteLine(Loc.GetString($"cmd-addrole-role-not-found"));
                return;
            }

            if (_jobSystem.MindHasJobWithId(mind, jobPrototype.Name))
            {
                shell.WriteLine(Loc.GetString($"cmd-addrole-mind-already-has-role"));
                return;
            }

            // Far Horizons, add faction if any
            if (args.Length > 2 && _prototypeManager.TryIndex<FactionPrototype>(args[2], out var _))
            {
                _jobSystem.MindAddJob(mind.Value, args[1], args[2]);
                return;
            }
            _jobSystem.MindAddJob(mind.Value, args[1]);
        }
    }
}
