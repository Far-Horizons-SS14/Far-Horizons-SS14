using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server._FarHorizons.Factions;
using Content.Server.Ghost.Roles;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AnyCommand]
    sealed class JoinGameCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
        [Dependency] private readonly IServerFactionManager _factions = default!; // Far Horizons
        [Dependency] private readonly ILogManager _logManager = default!;

        private readonly ISawmill _sawmill;

        public string Command => "joingame";
        public string Description => "";
        public string Help => "";

        public JoinGameCommand()
        {
            IoCManager.InjectDependencies(this);

            _sawmill = _logManager.GetSawmill("security");
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 4) // Far Horizons - +1 argument for faction
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            var player = shell.Player;

            if (player == null)
            {
                return;
            }

            var ticker = _entManager.System<GameTicker>();
            var stationJobs = _entManager.System<StationJobsSystem>();

            if (ticker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                shell.WriteLine("Round has not started.");
                return;
            }
            else if (ticker.RunLevel == GameRunLevel.InRound)
            {
                if (!int.TryParse(args[0], out var charSlot))
                {
                    shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                }

                // Far Horizons faction
                ProtoId<FactionPrototype> faction = args[1];

                ProtoId<JobPrototype> job = args[2];

                if (!int.TryParse(args[3], out var sid))
                {
                    shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                }
                
                if (ticker.PlayerGameStatuses.TryGetValue(player.UserId, out var status) && status == PlayerGameStatus.JoinedGame)
                {
                    //🌟Starlight🌟 start
                    var newLifeSystem = _entManager.System<NewLifeSystem>();

                    if (!newLifeSystem.SlotIsAvailable(player.UserId, charSlot))
                    {
                        Logger.InfoS("security", $"{player.Name} ({player.UserId}) attempted to latejoin while in-game.");
                        shell.WriteError($"{player.Name} is not in the lobby.   This incident will be reported.");
                        return;
                    }
                    //🌟Starlight🌟 end
                }

                var station = _entManager.GetEntity(new NetEntity(sid));
                var jobPrototype = _prototypeManager.Index<JobPrototype>(job);
                if(stationJobs.TryGetJobSlot(station, jobPrototype, out var slots) == false || slots == 0)
                {
                    shell.WriteLine($"{_factions.OverrideLocalizedJobName((faction, job))} has no available slots.");
                    return;
                }

                if (!_preferencesManager.GetPreferences(player.UserId).TryGetHumanoidInSlot(charSlot, out var humanoid))
                {
                    shell.WriteLine("No profile in slot");
                    return;
                }

                if (_adminManager.IsAdmin(player) && _cfg.GetCVar(CCVars.AdminDeadminOnJoin))
                {
                    _adminManager.DeAdmin(player);
                }

                if (!_factions.ListSpawnableFactionIDs().Contains(faction)){
                    shell.WriteLine("Faction can not be spawned");
                    return;
                }

                ticker.MakeJoinGame(player, humanoid, station, faction, job); // Far Horizons
                return;
            }

            ticker.MakeJoinGame(player, EntityUid.Invalid);
        }
    }
}
