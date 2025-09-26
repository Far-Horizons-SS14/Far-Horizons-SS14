using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.CCOperations.Systems;

public abstract class SharedCCOperationsSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public struct CCOperativeAgent(int id, GeneralStationRecord targetInfo)
{
    public int Id = id;
    public GeneralStationRecord TargetInfo = targetInfo;
    public bool UplinkOpen = false;
}

[ByRefEvent]
public readonly struct CCAgentInitializedEvent
{
    public readonly EntityUid AgentId;

    public CCAgentInitializedEvent(EntityUid agentId)
    {
        AgentId = agentId;
    }
}

