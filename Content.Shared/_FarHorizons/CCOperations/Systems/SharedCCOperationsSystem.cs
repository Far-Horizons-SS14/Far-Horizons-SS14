using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.CCOperations.Systems;

public abstract class SharedCCOperationsSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public struct CCOperativeAgent(GeneralStationRecord targetInfo)
{
    public GeneralStationRecord TargetInfo = targetInfo;
    public bool UplinkOpen = false;
}
