using System.Linq;
using Content.Shared._FarHorizons.DiscordLink;
using Robust.Shared.Network;

namespace Content.Client._FarHorizons.DiscordLink;

public sealed class DiscordLinkManager
{
    private string? _discordLink;
    private bool _isMentor;
    private AdditionalPermissionsTypes[] _permissions = [];
    
    [Dependency] private readonly IClientNetManager _netMgr = default!;
    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgDiscordLink>(OnGetDiscordLink);
        _netMgr.RegisterNetMessage<MsgPermissions>(OnGetPermissions);
    }
    
    private void OnGetDiscordLink(MsgDiscordLink message) => _discordLink = message.DiscordLink;

    private void OnGetPermissions(MsgPermissions message)
    {
        _permissions = message.Permissions;
        _isMentor = _permissions.Contains(AdditionalPermissionsTypes.Mentor);
    }

    public bool IsMentor() => _isMentor;
    
    public string? GetDiscordLink() => _discordLink;
}
