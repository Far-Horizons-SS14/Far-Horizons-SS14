namespace Content.Shared._FarHorizons.DiscordLink;

public interface IDiscordLinkManagerShared
{
    public bool HasPermission(EntityUid userEntityUid, AdditionalPermissionsTypes permission);
    public bool HasPermission(Guid userId, AdditionalPermissionsTypes permission);
}
