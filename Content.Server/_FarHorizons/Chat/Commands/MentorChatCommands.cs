using System.Linq;
using Content.Server._FarHorizons.DiscordLink;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._FarHorizons.Chat.Commands
{
    [AnyCommand]
    internal sealed partial class MentorChatCommand : LocalizedCommands
    {
        [Dependency] private IChatManager _chatManager = default!;
        [Dependency] private IDiscordLinkManager _discordLink = default!;
        [Dependency] private IAdminManager _admin = default!;

        public override string Command => "msay";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;

            if (player == null)
            {
                shell.WriteError(Loc.GetString($"shell-cannot-run-command-from-server"));
                return;
            }

            if (!_discordLink.Mentors.Contains(player) && !_admin.HasAdminFlag(player, AdminFlags.Adminchat))
            {
                shell.WriteError(Loc.GetString($"shell-cannot-speak-mentor"));
                return;
            }

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            _chatManager.TrySendOOCMessage(player, message, OOCChatType.Mentor);
        }
    }
}
