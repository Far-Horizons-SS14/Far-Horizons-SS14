using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Shuttles;

[RegisterComponent]
public sealed partial class BulletTracerSenderComponent : Component
{
    [DataField] public Color Color = Color.Orange;
}

[RegisterComponent]
public sealed partial class BulletTracerReceiverComponent : Component
{
    [DataField] public TimeSpan RefreshRate = TimeSpan.FromSeconds(0.5);

    [ViewVariables] public TimeSpan NextRefresh = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public sealed class BulletTracerPingMessage(List<(Vector2, Color)> pings) : BoundUserInterfaceMessage
{
    public List<(Vector2, Color)> Pings = pings;
}