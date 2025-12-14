using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.DiscordLink;

public sealed class MsgPermissions : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public bool IsMentor;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) => IsMentor = buffer.ReadBoolean();

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) => buffer.Write(IsMentor);
}