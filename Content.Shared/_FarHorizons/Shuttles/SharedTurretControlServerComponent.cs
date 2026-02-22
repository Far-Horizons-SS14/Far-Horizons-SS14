using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Shuttles;

[Serializable, NetSerializable]
public sealed class TurretControlSetTargetMessage(Vector2? position) : BoundUserInterfaceMessage
{
    public Vector2? Position { get; } = position;
}

[Serializable, NetSerializable]
public sealed class TurretControlFireCannonsMessage() : BoundUserInterfaceMessage;