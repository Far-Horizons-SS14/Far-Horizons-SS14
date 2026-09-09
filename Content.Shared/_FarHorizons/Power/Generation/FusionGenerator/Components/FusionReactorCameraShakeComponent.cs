using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FusionReactorCameraShakeComponent : Component
{
    [DataField]
    public int ShakeTimes = 10;

    [ViewVariables]
    public TimeSpan NextShake = TimeSpan.Zero;

    [DataField]
    public float Intensity = 15f;

    [DataField]
    public float Cooldown = 0.1f;
}