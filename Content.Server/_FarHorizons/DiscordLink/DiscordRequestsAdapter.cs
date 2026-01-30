using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Robust.Shared.Configuration;

namespace Content.Server._FarHorizons.DiscordLink;

public sealed class DiscordRequestsAdapter
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private readonly HttpClient _httpClient = new();
    private ISawmill _sawmill = default!;

    public sealed class DiscordRequestException(string message = "Failed.") : Exception(message);
    
    public void Initialize() => _sawmill = _logManager.GetSawmill("Discord Link Server");

    public async Task<string> GetDiscordToken(string oauthCode)
    {
        var tokenUrl = "https://discord.com/api/oauth2/token";
        var tokenData = new Dictionary<string, string>
        {
            { "client_id", _cfg.GetCVar(DiscordLinkCCVars.ClientId) },
            { "client_secret", _cfg.GetCVar(DiscordLinkCCVars.ClientSecret) },
            { "grant_type", "authorization_code" },
            { "code", oauthCode },
            { "redirect_uri", _cfg.GetCVar(DiscordLinkCCVars.RedirectUrl) }
        };

        var tokenContent = new FormUrlEncodedContent(tokenData);
        HttpResponseMessage tokenResponse;

        try
        {
            tokenResponse = await _httpClient.PostAsync(tokenUrl, tokenContent);
        }
        catch (Exception ex)
        {
            _sawmill.Error("Discord link failed: unexpected exception during token request.");
            _sawmill.Error(ex.ToString());
            throw new DiscordRequestException();
        }

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _sawmill.Error("Discord link failed: non-200 returned from token request.");
            throw new DiscordRequestException();
        }

        var tokenResponseJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenDataJson = JsonSerializer.Deserialize<JsonElement>(tokenResponseJson);

        if (!tokenDataJson.TryGetProperty("access_token", out var accessTokenElement))
        {
            _sawmill.Error("Discord link failed: token was not provided by discord.");
            throw new DiscordRequestException();
        }

        var token = accessTokenElement.GetString();
        if (token == null)
        {
            _sawmill.Error("Discord link failed: token value was not provided by discord.");
            throw new Exception("Discord link failed: token was not provided by discord.");
        }

        return token;
    }

    public async Task<string> GetDiscordUserId(string accessToken)
    {
        
        var userUrl = "https://discord.com/api/v10/users/@me";
        var userRequest = new HttpRequestMessage(HttpMethod.Get, userUrl);
        userRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

        HttpResponseMessage userResponse;

        try
        {
            userResponse = await _httpClient.SendAsync(userRequest);
        }
        catch (Exception ex)
        {
            _sawmill.Error("Discord link failed: unexpected exception during user info request.");
            _sawmill.Error(ex.ToString());
            throw new DiscordRequestException();
        }

        if (!userResponse.IsSuccessStatusCode)
        {
            _sawmill.Error("Discord link failed: non-200 returned from user request.");
            throw new DiscordRequestException();
        }

        var userResponseJson = await userResponse.Content.ReadAsStringAsync();
        var userDataJson = JsonSerializer.Deserialize<JsonElement>(userResponseJson);

        if (!userDataJson.TryGetProperty("id", out var userIdElement))
        {
            _sawmill.Error("Discord link failed: no id key in the user info response.");
            throw new DiscordRequestException();
        }

        var discordUserId = userIdElement.GetString();
        if (discordUserId == null)
        {
            _sawmill.Error("Discord link failed: no id value in the user info response.");
            throw new DiscordRequestException();
        }
        return discordUserId;
    }
    
    public async Task<List<string>> GetUserRoles(string discordUserId)
    {
        string guildId = _cfg.GetCVar(DiscordLinkCCVars.GuildId);
        string botToken = _cfg.GetCVar(DiscordLinkCCVars.BotToken);
        var url = $"https://discord.com/api/v10/guilds/{guildId}/members/{discordUserId}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bot {botToken}");

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            _sawmill.Error("Discord link failed: unexpected exception during user roles request.");
            _sawmill.Error(ex.ToString());
            throw new DiscordRequestException();
        }

        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Error("Discord link failed: non-200 returned from user roles request.");
            throw new DiscordRequestException();
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var dataJson = JsonSerializer.Deserialize<JsonElement>(responseJson);

        var roles = new List<string>();
        if (dataJson.TryGetProperty("roles", out var rolesElement))
        {
            foreach (var role in rolesElement.EnumerateArray())
            {
                roles.Add(role.GetString()!);
            }
        }
        return roles;
    }
}