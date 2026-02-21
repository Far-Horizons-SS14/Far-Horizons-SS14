using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._FarHorizons.DiscordLink;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.DiscordLink;

public sealed class DiscordLinkManager : IDiscordLinkManager
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DiscordRequestsAdapter _requests = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    private ISawmill _sawmill = default!;

    private readonly ConcurrentDictionary<string, OAuthStateInfo> _oauthStates = new();
    private readonly ConcurrentDictionary<Guid, ulong[]> _usersRoles = new();
    private readonly ConcurrentDictionary<Guid, ICommonSession> _mentors = new();
    public IEnumerable<ICommonSession> Mentors => _mentors.Values;

    private List<DiscordRolePrototype> _rolePrototypes = new();
    private Dictionary<ulong, DiscordRolePrototype> _rolesDictionary = new Dictionary<ulong, DiscordRolePrototype>();
    
    public const int ExpireDelayMinutes = 5*60;  // just to be sure...
    
    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("Discord Link Manager");
        _netMgr.RegisterNetMessage<MsgDiscordLink>();
        _netMgr.RegisterNetMessage<MsgPermissions>();
        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        _rolePrototypes = _prototypeManager.GetInstances<DiscordRolePrototype>().Values.OrderBy(item => item.Order).ToList();
        _rolesDictionary = _rolePrototypes.ToDictionary(item => item.DiscordRoleId);
    }

    public void Shutdown() {}

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Connected)
        {
            _ = OnPlayerConnected(e.Session);
        }

        if (e.NewStatus == SessionStatus.Disconnected)
        {
            // that way players can reconnect if roles weren't synced
            _ = RemoveUserRoles(e.Session);
        }
    }

    private async Task OnPlayerConnected(ICommonSession session)
    {
        await SyncUserRoles(session);
        SendDiscordLink(session);
    }

    private void SendPermissions(ICommonSession session) =>
        _netMgr.ServerSendMessage(
            new MsgPermissions { Permissions = GetPermissions(session.UserId.UserId) },
            session.Channel);

    private void SendDiscordLink(ICommonSession session) =>
        _netMgr.ServerSendMessage(
            new MsgDiscordLink { DiscordLink = GetDiscordAuthLink(session.UserId.UserId) },
            session.Channel);

    private AdditionalPermissionsTypes[] GetPermissions(Guid userId)
    {
        var roles = GetDiscordRoleIds(userId);
        return roles?.SelectMany(role => _rolesDictionary.TryGetValue(role, out var data) 
                ? data.AdditionalPermissions ?? [] 
                : [])
            .Distinct()
            .ToArray() ?? [];
    }

    public AdditionalPermissionsTypes GetPermissionsBytes(NetUserId userId)
    {
        var permissions = GetPermissions(userId.UserId);
        if (permissions.Length == 0) return (AdditionalPermissionsTypes)0;
        return permissions.Aggregate((a, b) => a | b);
    }
    
    public bool HasPermission(Guid userId, AdditionalPermissionsTypes permission)
    {
        var roles = GetDiscordRoleIds(userId);
        if (roles == null || roles.Length == 0) return false;
        foreach (var role in roles)
            if (_rolesDictionary.TryGetValue(role, out var value) &&
                value.AdditionalPermissions?.Contains(permission) == true)
                return true;
        return false;
    }

    public bool HasPermission(EntityUid userEntityUid, AdditionalPermissionsTypes permission) => 
        _playerManager.TryGetSessionByEntity(userEntityUid, out var session) 
        && HasPermission(session.UserId.UserId, permission);

    public bool IsMentor(Guid userId) => 
        _mentors.ContainsKey(userId) 
        || HasPermission(userId, AdditionalPermissionsTypes.Mentor);

    public List<string> ListMentorsNames() => Mentors.Select(item => item.Name).ToList();

    private static string GenerateStateToken(int length = 32)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public string GetDiscordAuthLink(Guid userId)
    {
        var state = GenerateStateToken(32);

        SetState(state, userId);
        
        const string BaseUrl = "https://discord.com/api/oauth2/authorize";
        var queryParams = new Dictionary<string, string>
        {
            { "client_id", _cfg.GetCVar(DiscordLinkCCVars.ClientId) },
            { "redirect_uri", _cfg.GetCVar(DiscordLinkCCVars.RedirectUrl) },
            { "response_type", "code" },
            { "scope", "identify" },
            { "state", state }
        };

        var queryString = string.Join("&", queryParams.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        var authUrl = $"{BaseUrl}?{queryString}";

        return authUrl;
    }

    public ulong[]? GetDiscordRoleIds(Guid userId)
    {
        if (_usersRoles.TryGetValue(userId, out var value))
            return (ulong[])value.Clone();

        return null;
    }

    public string GetDiscordRoleHighestTitle(Guid userId)
    {
        var userRoles = GetDiscordRoleIds(userId);
        if (userRoles == null) return "Staff";

        foreach (var role in _rolePrototypes.Where(role => userRoles.Contains(role.DiscordRoleId)))
            return role.PlayerTitle;

        return "Staff";
    }

    public async Task LinkDiscord(string state, Guid userId, string discordId)
    {
        await _dbManager.UpsertUserDiscordAsync(new NetUserId(userId), discordId);
        if (_playerManager.TryGetSessionById(new NetUserId(userId), out var session))
            await SyncUserRoles(session);
        RemoveState(state);
    }

    private async Task<string?> GetDiscordUserId(Guid userId)
    {
        var discordId = await _dbManager.GetUserDiscordAsync(new NetUserId(userId));
        return discordId?.DiscordId;
    }

    private async Task SyncUserRoles(ICommonSession session)
    {
        var userId = session.UserId.UserId;
        var discordId = await GetDiscordUserId(userId);
        if (discordId == null)
            return;
        var roles = await _requests.GetUserRoles(discordId);
        _usersRoles[userId] = roles.Select(ulong.Parse).ToArray();
        SendPermissions(session);
        if (IsMentor(userId))
            _mentors.TryAdd(session.UserId.UserId, session);
    }

    private async Task RemoveUserRoles(ICommonSession session) 
    {
        _usersRoles.TryRemove(session.UserId.UserId, out _);
        _mentors.TryRemove(session.UserId.UserId, out _);
    }

    private void CleanupStates()
    {
        var keys = _oauthStates.Keys.ToList();
        foreach (var key in keys)
        {
            _oauthStates.TryGetValue(key, out var stateInfo);
            if (stateInfo != null && stateInfo.ExpiresAt < DateTime.UtcNow) RemoveState(key);
        }
    }
    
    private void SetState(string state, Guid serviceUserId)
    {
        CleanupStates();

        if (string.IsNullOrEmpty(state))
            throw new ArgumentException("State cannot be null or empty");

        var stateInfo = new OAuthStateInfo
        {
            ServiceUserId = serviceUserId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ExpireDelayMinutes)
        };
        _oauthStates.TryAdd(state, stateInfo);
    }

    private void RemoveState(string state) => _oauthStates.Remove(state, out _);

    public OAuthStateInfo? GetState(string state)
    {
        _oauthStates.TryGetValue(state, out var stateInfo);
        if (stateInfo != null && stateInfo.ExpiresAt < DateTime.UtcNow)
        {
            RemoveState(state);
            return null;
        }

        return stateInfo;
    }
}