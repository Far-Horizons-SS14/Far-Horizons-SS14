using System.Linq;
using Content.Shared._FarHorizons.DiscordLink;
using Robust.Client.Player;
using Robust.Shared.Network;

namespace Content.Client._FarHorizons.DiscordLink;

public sealed partial class DiscordLinkManager: IDiscordLinkManagerShared
{
    private string? _discordLink;
    private bool _isMentor;
    private AdditionalPermissionsTypes[] _permissions = [];
    
    [Dependency] private IClientNetManager _netMgr = default!;
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

    public bool HasPermission(EntityUid userEntityUid, AdditionalPermissionsTypes permission)
        => _permissions.Contains(permission);

    public bool HasPermission(Guid userId, AdditionalPermissionsTypes permission) 
        => _permissions.Contains(permission);
}
