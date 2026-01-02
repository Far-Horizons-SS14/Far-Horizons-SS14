using System.Linq;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.DiscordLink;

public sealed class MsgPermissions : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public AdditionalPermissionsTypes[] Permissions = [];


    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) => 
        Permissions = ParsePermissions(buffer.ReadString());

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) => 
        buffer.Write(FormatPermissions());

    private static AdditionalPermissionsTypes[] ParsePermissions(string permissionsString)
    {
        if (string.IsNullOrWhiteSpace(permissionsString)) return [];

        if (Enum.TryParse<AdditionalPermissionsTypes>(permissionsString, out var combined))
            return Enum.GetValues<AdditionalPermissionsTypes>()
                .Where(p => combined.HasFlag(p)).ToArray();

        return [];
    }

    private string FormatPermissions()
    {
        if (Permissions.Length == 0) return "";
        var combined = Permissions.Aggregate<AdditionalPermissionsTypes, AdditionalPermissionsTypes>(
            0, (current, p) => current | p);
        return combined.ToString().Replace(" ", "");
    }
}