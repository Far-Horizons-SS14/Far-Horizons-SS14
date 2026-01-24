using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.DiscordLink;

public sealed partial class PermissionRequirement : JobRequirement
{
    
    [DataField(required: true)]
    public AdditionalPermissionsTypes Permission;

    public override bool Check(IEntityManager entManager,
        ICommonSession? player,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan>? playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();
        reason.AddMarkupOrThrow(Loc.GetString("role-insufficient-permissions", ("permission", Permission.ToString())));

        if (player == null) 
            return false;

        return IoCManager.Resolve<IDiscordLinkManagerShared>().HasPermission(player.UserId.UserId, Permission);
    }
}