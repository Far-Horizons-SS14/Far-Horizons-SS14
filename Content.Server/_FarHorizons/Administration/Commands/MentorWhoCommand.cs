using Content.Server._FarHorizons.DiscordLink;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._FarHorizons.Administration.Commands;

[AdminCommand(AdminFlags.Permissions)]
public sealed class MentorWhoCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IDiscordLinkManager _discordLink = default!;

    public string Command => "mentorwho";
    public string Description => Loc.GetString("list-mentors-command-description");

    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteLine(Loc.GetString("list-mentors-wrong-args-error"));
            return;
        }

        ListMentors(shell);
    }

    private async void ListMentors(IConsoleShell shell)
    {
        var mentors = _discordLink.ListMentorsNames();
            
        shell.WriteLine(Loc.GetString("list-mentors-success"));
        foreach (var mentor in mentors)
            shell.WriteLine(mentor);
    }
}