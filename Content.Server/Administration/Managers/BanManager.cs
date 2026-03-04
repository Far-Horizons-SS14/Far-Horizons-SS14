using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

#region Starlight
using System.Net.Http.Json;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Content.Server.Discord;
using Content.Shared.Starlight.CCVar;
using Robust.Shared;
using CCVars = Content.Shared.CCVar.CCVars;
#endregion Starlight

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager : IBanManager, IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly UserDbDataManager _userDbData = default!;

    private ISawmill _sawmill = default!;

    public const string SawmillId = "admin.bans";
    public const string DbTypeAntag = "Antag";
    public const string DbTypeJob = "Job";

    private const string WebhookLogo16X16 = "https://i.imgur.com/Q3mC5k1.png";
    private const string WebhookLogo128X128 = "https://i.imgur.com/eeMDddo.png";
    
    private readonly HttpClient _httpClient = new();
    private string _serverName = string.Empty;
    private string _webhookUrl = string.Empty;
    private WebhookData? _webhookData;
    private string _webhookName = "Punishments";
    private string _webhookAvatarUrl = WebhookLogo128X128;

    private readonly Dictionary<ICommonSession, List<BanDef>> _cachedRoleBans = new();
    // Cached ban exemption flags are used to handle
    private readonly Dictionary<ICommonSession, ServerBanExemptFlags> _cachedBanExemptions = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgRoleBans>();

        _db.SubscribeToJsonNotification<BanNotificationData>(
            _taskManager,
            _sawmill,
            BanNotificationChannel,
            ProcessBanNotification,
            OnDatabaseNotificationEarlyFilter);

        _cfg.OnValueChanged(StarlightCCVars.DiscordBanWebhook, OnWebhookChanged, true);
        _cfg.OnValueChanged(CVars.GameHostName, OnServerNameChanged, true);

        _userDbData.AddOnLoadPlayer(CachePlayerData);
        _userDbData.AddOnPlayerDisconnect(ClearPlayerData);
    }

    private async Task CachePlayerData(ICommonSession player, CancellationToken cancel)
    {
        var flags = await _db.GetBanExemption(player.UserId, cancel);

        var netChannel = player.Channel;
        ImmutableArray<byte>? hwId = netChannel.UserData.HWId.Length == 0 ? null : netChannel.UserData.HWId;
        var modernHwids = netChannel.UserData.ModernHWIds;
        var roleBans = await _db.GetBansAsync(
            netChannel.RemoteEndPoint.Address,
            player.UserId,
            hwId,
            modernHwids,
            false,
            type: BanType.Role);

        var userRoleBans = new List<BanDef>();
        foreach (var ban in roleBans)
        {
            userRoleBans.Add(ban);
        }

        cancel.ThrowIfCancellationRequested();
        _cachedBanExemptions[player] = flags;
        _cachedRoleBans[player] = userRoleBans;

        SendRoleBans(player);
    }

    private void ClearPlayerData(ICommonSession player)
    {
        _cachedBanExemptions.Remove(player);
    }

    public void Restart()
    {
        // Clear out players that have disconnected.
        var toRemove = new ValueList<ICommonSession>();
        foreach (var player in _cachedRoleBans.Keys)
        {
            if (player.Status == SessionStatus.Disconnected)
                toRemove.Add(player);
        }

        foreach (var player in toRemove)
        {
            _cachedRoleBans.Remove(player);
        }

        // Check for expired bans
        foreach (var roleBans in _cachedRoleBans.Values)
        {
            roleBans.RemoveAll(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }
    }

    #region Server Bans
    public async void CreateServerBan(CreateServerBanInfo banInfo)
    {
        var (banDef, expires) = await CreateBanDef(banInfo, BanType.Server, null);

        await _db.AddBanAsync(banDef);

        if (_cfg.GetCVar(CCVars.ServerBanResetLastReadRules))
        {
            // Reset their last read rules. They probably need a refresher!
            foreach (var (userId, _) in banInfo.Users)
            {
                await _db.SetLastReadRules(userId, null);
            }
        }

        var adminName = banInfo.BanningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banInfo.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");

        var targetName = banInfo.Users.Count == 0
            ? "null"
            : string.Join(", ", banInfo.Users.Select(u => $"{u.UserName} ({u.UserId})"));

        var addressRangeString = banInfo.AddressRanges.Count != 0
            ? "null"
            : string.Join(", ", banInfo.AddressRanges.Select(a => $"{a.Address}/{a.Mask}"));

        var hwidString = banInfo.HWIds.Count == 0
            ? "null"
            : string.Join(", ", banInfo.HWIds);

        var expiresString = expires == null ? Loc.GetString("server-ban-string-never") : $"{expires}";

        var key = _cfg.GetCVar(CCVars.AdminShowPIIOnBan) ? "server-ban-string" : "server-ban-string-no-pii";

        var logMessage = Loc.GetString(
            key,
            ("admin", adminName),
            ("severity", banDef.Severity),
            ("expires", expiresString),
            ("name", targetName),
            ("ip", addressRangeString),
            ("hwid", hwidString),
            ("reason", banInfo.Reason));

        _sawmill.Info(logMessage);
        _chat.SendAdminAlert(logMessage);

        var minutes = (uint)(banInfo.Duration?.TotalMinutes ?? 0);
        var ban = await _db.GetBanAsync(null, banInfo.Users.First().UserId, null, null);
        if (ban != null)
            SendWebhook(await GenerateBanPayload(ban, minutes));

        KickMatchingConnectedPlayers(banDef, "newly placed ban");
    }

    private NoteSeverity GetSeverityForServerBan(CreateBanInfo banInfo, CVarDef<string> defaultCVar)
    {
        if (banInfo.Severity != null)
            return banInfo.Severity.Value;

        if (Enum.TryParse(_cfg.GetCVar(defaultCVar), true, out NoteSeverity parsedSeverity))
            return parsedSeverity;

        _sawmill.Error($"CVar {defaultCVar.Name} has invalid ban severity!");
        return NoteSeverity.None;
    }

    private void KickMatchingConnectedPlayers(BanDef def, string source)
    {
        foreach (var player in _playerManager.Sessions)
        {
            if (BanMatchesPlayer(player, def))
            {
                KickForBanDef(player, def);
                _sawmill.Info($"Kicked player {player.Name} ({player.UserId}) through {source}");
            }
        }
    }

    private bool BanMatchesPlayer(ICommonSession player, BanDef ban)
    {
        var playerInfo = new BanMatcher.PlayerInfo
        {
            UserId = player.UserId,
            Address = player.Channel.RemoteEndPoint.Address,
            HWId = player.Channel.UserData.HWId,
            ModernHWIds = player.Channel.UserData.ModernHWIds,
            // It's possible for the player to not have cached data loading yet due to coincidental timing.
            // If this is the case, we assume they have all flags to avoid false-positives.
            ExemptFlags = _cachedBanExemptions.GetValueOrDefault(player, ServerBanExemptFlags.All),
            IsNewPlayer = false,
        };

        return BanMatcher.BanMatches(ban, playerInfo);
    }

    private void KickForBanDef(ICommonSession player, BanDef def)
    {
        var message = def.FormatBanMessage(_cfg, _localizationManager);
        player.Channel.Disconnect(message);
    }

    #endregion

    #region Role Bans

    public async void CreateRoleBan(CreateRoleBanInfo banInfo)
    {
        ImmutableArray<BanRoleDef> roleDefs =
        [
            .. ToBanRoleDef(banInfo.JobPrototypes),
            .. ToBanRoleDef(banInfo.AntagPrototypes),
        ];

        if (roleDefs.Length == 0)
            throw new ArgumentException("Must specify at least one role to ban!");

        var (banDef, expires) = await CreateBanDef(banInfo, BanType.Role, roleDefs);

        await AddRoleBan(banDef);

        var length = expires == null
            ? Loc.GetString("cmd-roleban-inf")
            : Loc.GetString("cmd-roleban-until", ("expires", expires));

        var targetName = banInfo.Users.Count == 0
            ? "null"
            : string.Join(", ", banInfo.Users.Select(u => $"{u.UserName} ({u.UserId})"));

        _chat.SendAdminAlert(Loc.GetString(
            "cmd-roleban-success",
            ("target", targetName),
            ("role", string.Join(", ", roleDefs)),
            ("reason", banInfo.Reason),
            ("length", length)));

        foreach (var (userId, _) in banInfo.Users)
        {
            if (_playerManager.TryGetSessionById(userId, out var session))
                SendRoleBans(session);
        }
    }

    private async Task<(BanDef Ban, DateTimeOffset? Expires)> CreateBanDef(
        CreateBanInfo banInfo,
        BanType type,
        ImmutableArray<BanRoleDef>? roleBans)
    {
        if (banInfo.Users.Count == 0 && banInfo.HWIds.Count == 0 && banInfo.AddressRanges.Count == 0)
            throw new ArgumentException("Must specify at least one user, HWID, or address range");

        DateTimeOffset? expires = null;
        if (banInfo.Duration is { } duration)
            expires = DateTimeOffset.Now + duration;

        ImmutableArray<int> roundIds;
        if (banInfo.RoundIds.Count > 0)
        {
            roundIds = [..banInfo.RoundIds];
        }
        else if (_systems.TryGetEntitySystem<GameTicker>(out var ticker) && ticker.RoundId != 0)
        {
            roundIds = [ticker.RoundId];
        }
        else
        {
            roundIds = [];
        }

        return (new BanDef(
            null,
            type,
            [..banInfo.Users.Select(u => u.UserId)],
            [..banInfo.AddressRanges],
            [..banInfo.HWIds],
            DateTimeOffset.Now,
            expires,
            roundIds,
            await GetPlayTime(banInfo),
            banInfo.Reason,
            GetSeverityForServerBan(banInfo, CCVars.ServerBanDefaultSeverity),
            banInfo.BanningAdmin,
            null,
            roles: roleBans), expires);
    }

    private async Task<TimeSpan> GetPlayTime(CreateBanInfo banInfo)
    {
        var firstPlayer = banInfo.Users.FirstOrNull()?.UserId;
        if (firstPlayer == null)
            return TimeSpan.Zero;

        return (await _db.GetPlayTimes(firstPlayer.Value))
            .Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)
            ?.TimeSpent ?? TimeSpan.Zero;
    }

    private IEnumerable<BanRoleDef> ToBanRoleDef<T>(IEnumerable<ProtoId<T>> protoIds) where T : class, IPrototype
    {
        return protoIds.Select(protoId =>
        {
            // TODO: I have no idea if this check is necessary. The previous code was a complete mess,
            // so out of safety I'm leaving this in.
            if (_prototypeManager.HasIndex<JobPrototype>(protoId) && _prototypeManager.HasIndex<AntagPrototype>(protoId))
            {
                throw new InvalidOperationException(
                    $"Creating role ban for {protoId}: cannot create role ban, role is both JobPrototype and AntagPrototype.");
            }

            // Don't trust the input: make sure the role actually exists.
            if (!_prototypeManager.HasIndex(protoId))
                throw new UnknownPrototypeException(protoId, typeof(T));

            return new BanRoleDef(PrototypeKindToDbType<T>(), protoId);
        });
    }

    private static string PrototypeKindToDbType<T>() where T : class, IPrototype
    {
        if (typeof(T) == typeof(JobPrototype))
            return DbTypeJob;

        if (typeof(T) == typeof(AntagPrototype))
            return DbTypeAntag;

        throw new ArgumentException($"Unknown prototype kind for role bans: {typeof(T)}");
    }

    private async Task AddRoleBan(BanDef banDef)
    {
        banDef = await _db.AddBanAsync(banDef);

        foreach (var user in banDef.UserIds)
        {
            if (_playerManager.TryGetSessionById(user, out var player)
                && _cachedRoleBans.TryGetValue(player, out var cachedBans))
            {
                cachedBans.Add(banDef);
            }
        }
    }

    public async void WebhookUpdateRoleBans(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableTypedHwid? hwid, IReadOnlyCollection<string> roles, uint? minutes, NoteSeverity severity, string reason, DateTimeOffset timeOfBan, ImmutableArray<BanRoleDef>? banRoleDefs = null)
    {
        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);
        }

        List<NetUserId> targets = [];
        if (target != null)
            targets.Add(target.Value);

        List<(IPAddress, int)> addresses = [];
        if (addressRange != null)
            addresses.Add(addressRange.Value);
        
        List<ImmutableTypedHwid> hwids = [];
        if (hwid != null)
            hwids.Add(hwid);
        
        List<int> roundIds = [];
        if (roundId != null)
            roundIds.Add(roundId.Value);

        var banDef = new BanDef(
            null,
            BanType.Role,
            [..targets],
            [..addresses],
            [..hwids],
            timeOfBan,
            expires,
            [..roundIds],
            playtime,
            reason,
            severity,
            banningAdmin,
            null, 
            ServerBanExemptFlags.None,
            banRoleDefs);

        SendWebhook(await GenerateJobBanPayload(banDef, roles, minutes));
    }

    public async Task<string> PardonRoleBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
    {
        var ban = await _db.GetBanAsync(banId);

        if (ban == null)
        {
            return $"No ban found with id {banId}";
        }

        if (ban.Type != BanType.Role)
            throw new InvalidOperationException("Ban was not a role ban!");

        if (ban.Unban != null)
        {
            var response = new StringBuilder("This ban has already been pardoned");

            if (ban.Unban.UnbanningAdmin != null)
            {
                response.Append($" by {ban.Unban.UnbanningAdmin.Value}");
            }

            response.Append($" in {ban.Unban.UnbanTime}.");
            return response.ToString();
        }

        await _db.AddUnbanAsync(new UnbanDef(banId, unbanningAdmin, DateTimeOffset.Now));

        foreach (var user in ban.UserIds)
        {
            if (_playerManager.TryGetSessionById(user, out var session)
                && _cachedRoleBans.TryGetValue(session, out var roleBans))
            {
                roleBans.RemoveAll(roleBan => roleBan.Id == ban.Id);
                SendRoleBans(session);
            }

        }

        return $"Pardoned ban with id {banId}";
    }

    public HashSet<ProtoId<JobPrototype>>? GetJobBans(NetUserId playerUserId)
    {
        return GetRoleBans<JobPrototype>(playerUserId);
    }

    public HashSet<ProtoId<AntagPrototype>>? GetAntagBans(NetUserId playerUserId)
    {
        return GetRoleBans<AntagPrototype>(playerUserId);
    }

    private HashSet<ProtoId<T>>? GetRoleBans<T>(NetUserId playerUserId) where T : class, IPrototype
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        return GetRoleBans<T>(session);
    }

    private HashSet<ProtoId<T>>? GetRoleBans<T>(ICommonSession playerSession) where T : class, IPrototype
    {
        if (!_cachedRoleBans.TryGetValue(playerSession, out var roleBans))
            return null;

        var dbType = PrototypeKindToDbType<T>();

        return roleBans
            .SelectMany(ban => ban.Roles!.Value)
            .Where(role => role.RoleType == dbType)
            .Select(role => new ProtoId<T>(role.RoleId))
            .ToHashSet();
    }

    public HashSet<BanRoleDef>? GetRoleBans(NetUserId playerUserId)
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        return _cachedRoleBans.TryGetValue(session, out var roleBans)
            ? roleBans.SelectMany(banDef => banDef.Roles ?? []).ToHashSet()
            : null;
    }

    public bool IsRoleBanned(ICommonSession player, List<ProtoId<JobPrototype>> jobs)
    {
        return IsRoleBanned<JobPrototype>(player, jobs);
    }

    public bool IsRoleBanned(ICommonSession player, List<ProtoId<AntagPrototype>> antags)
    {
        return IsRoleBanned<AntagPrototype>(player, antags);
    }

    private bool IsRoleBanned<T>(ICommonSession player, List<ProtoId<T>> roles) where T : class, IPrototype
    {
        var bans = GetRoleBans(player.UserId);

        if (bans is null || bans.Count == 0)
            return false;

        var dbType = PrototypeKindToDbType<T>();

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var role in roles)
        {
            if (bans.Contains(new BanRoleDef(dbType, role)))
                return true;
        }

        return false;
    }

    public void SendRoleBans(ICommonSession pSession)
    {
        var bans = new MsgRoleBans()
        {
            JobBans = (GetRoleBans<JobPrototype>(pSession) ?? []).ToList(),
            AntagBans = (GetRoleBans<AntagPrototype>(pSession) ?? []).ToList(),
        };

        _sawmill.Debug($"Sent role bans to {pSession.Name}");
        _netManager.ServerSendMessage(bans, pSession.Channel);
    }

    #endregion

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }

    #region Webhook
    private async void SendWebhook(WebhookPayload payload)
    {
        if (_webhookUrl == string.Empty) return;

        var request = await _httpClient.PostAsync($"{_webhookUrl}?wait=true",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        var content = await request.Content.ReadAsStringAsync();
        if (!request.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message (perhaps the message is too long?): {request.StatusCode}\nResponse: {content}");
            return;
        }

        var id = JsonNode.Parse(content)?["id"];
        if (id == null)
        {
            _sawmill.Log(LogLevel.Error, $"Could not find id in json-content returned from discord webhook: {content}");
            return;
        }
    }

    private async Task<WebhookPayload> GenerateJobBanPayload(BanDef banDef, IReadOnlyCollection<string> roles, uint? minutes = null)
    {
        var hwid = banDef.HWIds.Length > 0 ? string.Join(", ", banDef.HWIds) : "null";
        var adminName = banDef.BanningAdmin == null ? Loc.GetString("system-user") : (await _db.GetPlayerRecordByUserId(banDef.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");
        var targetName = banDef.UserIds.Length == 0 ? Loc.GetString("server-ban-no-name", ("hwid", hwid)) : (await _db.GetPlayerRecordByUserId(banDef.UserIds[0]))?.LastSeenUserName ?? Loc.GetString("server-ban-no-name", ("hwid", hwid));
        var expiresString = banDef.ExpirationTime == null ? Loc.GetString("server-ban-string-never") : "" + TimeZoneInfo.ConvertTimeFromUtc(banDef.ExpirationTime.Value.UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        var reason = banDef.Reason.Replace("\n", "\n> ").Trim() ?? "No reason provided";
        var id = banDef.Id;
        var round = string.Join(", ", banDef.RoundIds);
        var severity = "" + banDef.Severity;
        var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];
        var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        var rolesString = "";
        foreach (var role in roles)
            rolesString += $"\n> `{role}`";

        string? adminDiscordId = null;
        string? targetDiscordId = null;

        var adminLink = "";
        var targetLink = "";
        var mentions = new List<User> { };
        if (adminDiscordId != null)
        {
            adminLink = $"<@{adminDiscordId}>";
            mentions.Add(new User() { Id = adminDiscordId });
        }

        if (targetDiscordId != null)
        {
            targetLink = $"<@{targetDiscordId}>";
            mentions.Add(new User() { Id = targetDiscordId });
        }

        var allowedMentions = new Dictionary<string, string[]>
        {
            { "parse", new List<string> {"users"}.ToArray() }
        };

        if (banDef.ExpirationTime != null && minutes != null) // Time ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString("server-role-ban-string", ("targetName", targetName), ("targetLink", targetLink), ("adminLink", adminLink), ("adminName", adminName), ("TimeNow", timeNow), ("roles", rolesString), ("expiresString", expiresString), ("reason", reason), ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0x004281,
                        Thumbnail = new EmbedThumbnail
                        {
                            Url = "https://static.wikia.nocookie.net/ss14andromeda13/images/6/66/%D0%9E%D1%84%D0%B8%D1%86%D0%B5%D1%80_%D0%A1%D0%BB%D1%83%D0%B6%D0%B1%D1%8B_%D0%91%D0%B5%D0%B7%D0%BE%D0%BF%D0%B0%D1%81%D0%BD%D0%BE%D1%81%D1%82%D0%B8.png/revision/latest/scale-to-width-down/110?cb=20230216091617&path-prefix=ru",
                        },
                        Author = new EmbedAuthor
                        {
                            Name = Loc.GetString("server-role-ban", ("mins", minutes.Value)) + $"",
                            IconUrl = "https://cdn.discordapp.com/emojis/1129749368199712829.webp?size=40&quality=lossless" // BanHummer
                        },
                        Footer = new EmbedFooter
                        {
                            Text =  Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                            IconUrl = WebhookLogo16X16
                        },
                    },
                },
            };
        else // Perma ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString("server-perma-role-ban-string", ("targetName", targetName), ("targetLink", targetLink), ("adminLink", adminLink), ("adminName", adminName), ("TimeNow", timeNow), ("roles", rolesString), ("expiresString", expiresString), ("reason", reason), ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0xffb840,
                        Thumbnail = new EmbedThumbnail
                        {
                            Url = "https://static.wikia.nocookie.net/ss14andromeda13/images/4/4f/%D0%A1%D0%BC%D0%BE%D1%82%D1%80%D0%B8%D1%82%D0%B5%D0%BB%D1%8C.png/revision/latest?cb=20230216091556&path-prefix=ru",
                        },
                        Author = new EmbedAuthor
                        {
                            Name = $"{Loc.GetString("server-perma-role-ban")}",
                            IconUrl = "https://cdn.discordapp.com/emojis/1129749368199712829.webp?size=40&quality=lossless" // BanHummer
                        },
                        Footer = new EmbedFooter
                        {
                            Text = Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                            IconUrl = WebhookLogo16X16
                        },
                    },
                },
            };
    }

    private async Task<WebhookPayload> GenerateBanPayload(BanDef banDef, uint? minutes = null)
    {
        var hwid = banDef.HWIds.Length > 0 ? string.Join(", ", banDef.HWIds) : "null";
        var adminName = banDef.BanningAdmin == null ? Loc.GetString("system-user") : (await _db.GetPlayerRecordByUserId(banDef.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");
        var targetName = banDef.UserIds.Length == 0 ? Loc.GetString("server-ban-no-name", ("hwid", hwid)) : (await _db.GetPlayerRecordByUserId(banDef.UserIds[0]))?.LastSeenUserName ?? Loc.GetString("server-ban-no-name", ("hwid", hwid));
        var expiresString = banDef.ExpirationTime == null ? Loc.GetString("server-ban-string-never") : "" + TimeZoneInfo.ConvertTimeFromUtc(banDef.ExpirationTime.Value.UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        var reason = banDef.Reason.Replace("\n", "\n> ").Trim() ?? "No reason provided";
        var id = banDef.Id;
        var round = string.Join(", ", banDef.RoundIds);
        var severity = "" + banDef.Severity;
        var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];
        var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

        string? adminDiscordId = null;
        string? targetDiscordId = null;

        var adminLink = "";
        var targetLink = "";
        var mentions = new List<User> { };
        if (adminDiscordId != null)
        {
            adminLink = $"<@{adminDiscordId}>";
            mentions.Add(new User() { Id = adminDiscordId });
        }

        if (targetDiscordId != null)
        {
            targetLink = $"<@{targetDiscordId}>";
            mentions.Add(new User() { Id = targetDiscordId });
        }

        var allowedMentions = new Dictionary<string, string[]>
        {
            { "parse", new List<string> {"users"}.ToArray() }
        };

        if (banDef.ExpirationTime != null && minutes != null) // Time ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString("server-time-ban-string", ("targetName", targetName), ("targetLink", targetLink), ("adminLink", adminLink), ("adminName", adminName), ("TimeNow", timeNow), ("expiresString", expiresString), ("reason", reason), ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0x803045,
                        Thumbnail = new EmbedThumbnail
                        {
                            Url = "https://i.imgur.com/As5sGru.png",
                        },
                        Author = new EmbedAuthor
                        {
                            Name = Loc.GetString("server-time-ban", ("mins", minutes.Value)) + $" #{id}",
                            IconUrl = "https://cdn.discordapp.com/emojis/1129749368199712829.webp?size=40&quality=lossless" // BanHummer
                        },
                        Footer = new EmbedFooter
                        {
                            Text =  Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                            IconUrl = WebhookLogo16X16
                        },
                    },
                },
            };
        else // Perma ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString("server-perma-ban-string", ("targetName", targetName), ("targetLink", targetLink), ("adminLink", adminLink), ("adminName", adminName), ("TimeNow", timeNow), ("reason", reason), ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0x8B0000,
                        Thumbnail = new EmbedThumbnail
                        {
                            Url = "https://static.wikia.nocookie.net/ss14andromeda13/images/7/72/%D0%94%D0%B5%D1%82%D0%B5%D0%BA%D1%82%D0%B8%D0%B2.png/revision/latest?cb=20230216091637&path-prefix=ru",
                        },
                        Author = new EmbedAuthor
                        {
                            Name = $"{Loc.GetString("server-perma-ban")} #{id}",
                            IconUrl = "https://cdn.discordapp.com/emojis/1129749368199712829.webp?size=40&quality=lossless" // BanHummer
                        },
                        Footer = new EmbedFooter
                        {
                            Text = Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                            IconUrl = WebhookLogo16X16
                        },
                    },
                },
            };
    }

    private void OnWebhookChanged(string url)
    {
        _webhookUrl = url;

        if (url == string.Empty)
            return;

        // Basic sanity check and capturing webhook ID and token
        var match = Regex.Match(url, @"^https://discord\.com/api/webhooks/(\d+)/((?!.*/).*)$");

        if (!match.Success)
        {
            // TODO: Ideally, CVar validation during setting should be better integrated
            _sawmill.Warning("Webhook URL does not appear to be valid. Using anyways...");
            return;
        }

        if (match.Groups.Count <= 2)
        {
            _sawmill.Error("Could not get webhook ID or token.");
            return;
        }

        var webhookId = match.Groups[1].Value;
        var webhookToken = match.Groups[2].Value;

        // Fire and forget
        _ = SetWebhookData(webhookId, webhookToken);
    }

    private void OnServerNameChanged(string obj)
    {
        _serverName = obj;
        _webhookName = $"{_serverName} Punishments";
    }

    private async Task SetWebhookData(string id, string token)
    {
        var response = await _httpClient.GetAsync($"https://discord.com/api/v10/webhooks/{id}/{token}");

        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when trying to get webhook data (perhaps the webhook URL is invalid?): {response.StatusCode}\nResponse: {content}");
            return;
        }

        _webhookData = JsonSerializer.Deserialize<WebhookData>(content);
    }

    // https://discord.com/developers/docs/resources/channel#embed-object-embed-structure
    private struct Embed
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("color")]
        public int Color { get; set; } = 0;

        [JsonPropertyName("author")]
        public EmbedAuthor? Author { get; set; } = null;

        [JsonPropertyName("thumbnail")]
        public EmbedThumbnail? Thumbnail { get; set; } = null;

        [JsonPropertyName("footer")]
        public EmbedFooter? Footer { get; set; } = null;
        public Embed()
        {
        }
    }

    // https://discord.com/developers/docs/resources/channel#embed-object-embed-author-structure
    private struct EmbedAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        public EmbedAuthor()
        {
        }
    }

    // https://discord.com/developers/docs/resources/webhook#webhook-object-webhook-structure
    private struct WebhookData
    {
        [JsonPropertyName("guild_id")]
        public string? GuildId { get; set; } = null;

        [JsonPropertyName("channel_id")]
        public string? ChannelId { get; set; } = null;

        public WebhookData()
        {
        }
    }

    // https://discord.com/developers/docs/resources/channel#message-object-message-structure
    private struct WebhookPayload
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; } = "";

        [JsonPropertyName("embeds")]
        public List<Embed>? Embeds { get; set; } = null;

        [JsonPropertyName("mentions")]
        public List<User> Mentions { get; set; } = new();

        [JsonPropertyName("allowed_mentions")]
        public Dictionary<string, string[]> AllowedMentions { get; set; } =
            new()
            {
                    { "parse", Array.Empty<string>() },
            };

        public WebhookPayload()
        {
        }
    }

    // https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure
    private struct EmbedFooter
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        public EmbedFooter()
        {
        }
    }

    // https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure
    private struct EmbedThumbnail
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";
        public EmbedThumbnail()
        {
        }
    }

    private struct User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        public User()
        {
        }
    }
    #endregion
}
