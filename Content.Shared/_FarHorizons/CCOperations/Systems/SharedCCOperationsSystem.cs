using Robust.Shared.Serialization;

namespace Content.Shared.CCOperations.Systems;

public abstract class SharedCCOperationsSystem : EntitySystem
{
}

[ByRefEvent]
public readonly struct CCAgentInitializedEvent(CCOperativeAgent agent)
{
    public readonly CCOperativeAgent Agent = agent;
}

public readonly struct CCAgentUpdatedEvent(CCOperativeAgent agent)
{
    public readonly CCOperativeAgent Agent = agent;
}

[Serializable, NetSerializable]
public sealed class CCAgentToggleUplinkMessage(int agentId, bool uplinkStatus) : EntityEventArgs
{
    public readonly int AgentId = agentId;
    public readonly bool UplinkStatus = uplinkStatus;
}

[Serializable, NetSerializable]
public sealed class CCNeutralizeAgentMessage(int agentId) : EntityEventArgs
{
    public readonly int AgentId = agentId;
}

[Serializable, NetSerializable]
public struct CCOperativeAgent(
    int id,
    bool uplinkOpen,
    string name,
    int age,
    string jobTitle,
    string species,
    string gender
)
{
    public int Id = id;
    public bool UplinkOpen = uplinkOpen;
    public string Name = name;
    public int Age = age;
    public string JobTitle = jobTitle;
    public string Species = species;
    public string Gender = gender;
}
