using System.Collections.Specialized;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;

namespace Content.Server._FarHorizons.DiscordLink;

public sealed class DiscordOauthServer : IPostInjectInit
{
    [Dependency] private readonly IStatusHost _statusHost = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IDiscordLinkManager _discordLinkManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly DiscordRequestsAdapter _requests = default!;
    private ISawmill _sawmill = default!;

    private void RegisterHandler(HttpMethod method, string exactPath, Func<IStatusHandlerContext, Task> handler)
    {
        _statusHost.AddHandler(async context =>
        {
            if (context.RequestMethod != method || context.Url.AbsolutePath != exactPath)
                return false;

            await handler(context);
            return true;
        });
    }

    void IPostInjectInit.PostInject() => RegisterHandler(HttpMethod.Get, "/discord/oauth", DiscordOauth);

    private async Task<bool> RespondFail(IStatusHandlerContext context, string message = "Failed.", int code = 500)
    {
        await context.RespondAsync(message, code).ConfigureAwait(false);
        return false;  // for quick exit
    }

    private async Task<bool> DiscordOauth(IStatusHandlerContext context)
    {
        NameValueCollection queryParameters = HttpUtility.ParseQueryString(context.Url.Query);
        var code = queryParameters.Get("code");
        var state = queryParameters.Get("state");
        if (code == null || state == null)
        {
            _sawmill.Error("Discord link failed: callback got no params from discord.");
            return await RespondFail(context);
        }

        var oauthState = _discordLinkManager.GetState(state);
        if (oauthState == null)
        {
            _sawmill.Error("Discord link failed: token not found or expired.");
            return await RespondFail(context, "Invalid token.", 400);
        }

        var serviceUserId = oauthState.ServiceUserId;

        string discordUserId;
        try
        {
            string accessToken = await _requests.GetDiscordToken(code);
            discordUserId = await _requests.GetDiscordUserId(accessToken);
        }
        catch (DiscordRequestsAdapter.DiscordRequestException e)
        {
            return await RespondFail(context);
        }

        await _discordLinkManager.LinkDiscord(state, serviceUserId, discordUserId);
        
        await context.RespondAsync("Done!", 200).ConfigureAwait(false);
        return true;
    }

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("Discord Link Server");
    }
}