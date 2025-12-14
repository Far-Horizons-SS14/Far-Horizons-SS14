using System.Threading.Tasks;
using Robust.Shared.Player;

namespace Content.Server._FarHorizons.DiscordLink;

public interface IDiscordLinkManager
{
    void Initialize();
    void Shutdown();
    public string GetDiscordAuthLink(Guid userId);
    public ulong[]? GetDiscordRoleIds(Guid userId);
    public string GetDiscordRoleHighestTitle(Guid userId);
    public Task LinkDiscord(string state, Guid userId, string discordId);
    public OAuthStateInfo? GetState(string state);
    IEnumerable<ICommonSession> Mentors { get; }
}
