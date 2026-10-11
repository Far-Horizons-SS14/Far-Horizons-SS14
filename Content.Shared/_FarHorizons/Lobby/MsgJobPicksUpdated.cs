using Content.Shared._FarHorizons.Factions;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Lobby;

public sealed class MsgJobPicksUpdated : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Dictionary<ProtoId<FactionJobAssignmentPrototype>, (int, int, int)> JobPicks = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadVariableInt32();
        JobPicks = new Dictionary<ProtoId<FactionJobAssignmentPrototype>, (int, int, int)>(count);

        for (int i = 0; i < count; i++)
        {
            var protoId = buffer.ReadString();
            var high = buffer.ReadVariableInt32();
            var med = buffer.ReadVariableInt32();
            var low = buffer.ReadVariableInt32();
            
            JobPicks.Add(new ProtoId<FactionJobAssignmentPrototype>(protoId), (high, med, low));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(JobPicks.Count);
        
        foreach (var (protoId, picks) in JobPicks)
        {
            buffer.Write(protoId.Id);
            buffer.WriteVariableInt32(picks.Item1);
            buffer.WriteVariableInt32(picks.Item2);
            buffer.WriteVariableInt32(picks.Item3);
        }
    }
}