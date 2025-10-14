using Robust.Shared.Serialization;

namespace Content.Shared.CCOperations.Systems;

public abstract class SharedCCOperationsSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public struct CCOperativeAgent(int id)
{
    public int Id = id;  // expected to be character uid (basically user netId)
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

[Serializable, NetSerializable]
public struct CCOperativeAgentUiItem(
    int id,
    bool uplinkOpen,
    string name,
    int age,
    string jobTitle,
    string species,
    string gender,
    string state
)
{
    public int Id = id;
    public bool UplinkOpen = uplinkOpen;
    public string Name = name;
    public int Age = age;
    public string JobTitle = jobTitle;
    public string Species = species;
    public string Gender = gender;
    public string State = state;
}
