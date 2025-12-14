using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.DiscordLink;

public sealed class MsgDiscordLink : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public string? DiscordLink;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) => DiscordLink = buffer.ReadString();

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) => buffer.Write(DiscordLink);
}