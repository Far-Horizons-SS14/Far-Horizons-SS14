namespace Content.Server._FarHorizons.DiscordLink;


public sealed class OAuthStateInfo
{
    public required Guid ServiceUserId;
    public DateTime ExpiresAt;
}
