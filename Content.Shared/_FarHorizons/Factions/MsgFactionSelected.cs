using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.FarHorizons.Factions;

/// <summary>
/// The server sends this when faction is selected in lobby. Or when player joins server.
/// </summary>
public sealed class MsgFactionSelected : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public ProtoId<FactionPrototype>? Faction = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();
        using var stream = new MemoryStream();
        buffer.ReadAlignedMemory(stream, length);
        serializer.DeserializeDirect(stream, out string content);
        Faction = content == string.Empty ?
                    null :
                    (ProtoId<FactionPrototype>?)content;
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        var content = Faction == null ?
                        string.Empty :
                        (string)Faction;
        using var stream = new MemoryStream();
        serializer.SerializeDirect(stream, content);
        buffer.WriteVariableInt32((int)stream.Length);
        stream.TryGetBuffer(out var segment);
        buffer.Write(segment);
    }
}