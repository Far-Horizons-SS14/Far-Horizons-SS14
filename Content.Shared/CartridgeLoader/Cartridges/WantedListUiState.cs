using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class WantedListUiState(List<WantedRecord> records, string? targetName = null) : BoundUserInterfaceState //FarHorizons
{
    public List<WantedRecord> Records = records;
    public string? TargetName = targetName; //FarHorizons
}
