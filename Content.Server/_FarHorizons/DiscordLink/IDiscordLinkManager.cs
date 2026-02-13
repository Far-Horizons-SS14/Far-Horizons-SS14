using System.Threading.Tasks;
using Content.Shared._FarHorizons.DiscordLink;
using Robust.Shared.Player;

namespace Content.Server._FarHorizons.DiscordLink;

public interface IDiscordLinkManager: IDiscordLinkManagerShared
{
    void Initialize();
    void Shutdown();
    public string GetDiscordRoleHighestTitle(Guid userId);
    public Task LinkDiscord(string state, Guid userId, string discordId);
    public OAuthStateInfo? GetState(string state);
    IEnumerable<ICommonSession> Mentors { get; }
    public List<string> ListMentorsNames();
}
