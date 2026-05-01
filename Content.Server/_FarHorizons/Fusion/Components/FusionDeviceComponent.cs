namespace Content.Server._FarHorizons.Fusion.Components;

[RegisterComponent]
public sealed partial class FusionDeviceComponent : Component
{
    [ViewVariables]
    public TimeSpan LastProcess = TimeSpan.Zero;
}

[ByRefEvent]
public readonly struct FusionDeviceUpdateEvent(double dt)
{
    public readonly double dt = dt;
}