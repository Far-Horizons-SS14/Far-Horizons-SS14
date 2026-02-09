using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Shuttles;

[RegisterComponent]
public sealed partial class SpaceRescuePingSenderComponent : Component
{
    [DataField] public bool Hidden;
    [DataField] public Color Color = Color.Red;
}

[RegisterComponent]
public sealed partial class SpaceRescuePingReceiverComponent : Component
{
    [DataField] public TimeSpan RefreshRate = TimeSpan.FromSeconds(5);

    [ViewVariables] public TimeSpan NextRefresh = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public sealed class SpaceRescuePingMessage(List<(Vector2, Color)> pings, TimeSpan refreshRate) : BoundUserInterfaceMessage
{
    public List<(Vector2, Color)> Pings = pings;
    public TimeSpan RefreshRate = refreshRate;
}