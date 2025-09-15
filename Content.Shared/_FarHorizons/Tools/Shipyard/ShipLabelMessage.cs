using Robust.Shared.Serialization;

namespace Content.Shared.FarHorizons.Tools.Shipyard;

[Serializable, NetSerializable]
public sealed class ShipLabelerNameChangeRequest(string name) : BoundUserInterfaceMessage
{
    public string Name { get; } = name;
}

[Serializable, NetSerializable]
public sealed class ShipLabelerNameChangeResponse(bool success, string error = "") : BoundUserInterfaceMessage
{
    public bool Success { get; } = success;
    public string Error { get; } = error;
}