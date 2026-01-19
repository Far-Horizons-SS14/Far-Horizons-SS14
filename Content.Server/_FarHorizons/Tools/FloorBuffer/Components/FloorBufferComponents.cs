using Content.Server._FarHorizons.Tools.FloorBuffer.Systems;

namespace Content.Server._FarHorizons.Tools.FloorBuffer.Components;

[RegisterComponent]
[Access(typeof(FloorBufferSystem))]
public sealed partial class FloorBufferComponent : Component
{
    [DataField]
    public bool Enabled = false;
}