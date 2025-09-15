using Robust.Shared.Serialization;

namespace Content.Shared.FarHorizons.Tools.Shipyard;

/// <summary>
///     On interacting with an entity retrieves the entity UID for use with getting the current damage on an entity.
/// </summary>
[Serializable, NetSerializable]
public sealed class IntegrityAnalyzerScannedTargetMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;
    public bool? ScanMode;

    public IntegrityAnalyzerScannedTargetMessage(NetEntity? targetEntity, bool? scanMode)
    {
        TargetEntity = targetEntity;
        ScanMode = scanMode;
    }
}

