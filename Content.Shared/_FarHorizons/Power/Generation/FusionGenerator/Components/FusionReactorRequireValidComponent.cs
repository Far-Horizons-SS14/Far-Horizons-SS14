using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FusionReactorRequireValidComponent : Component
{
    public FusionReactorValidity Validity = FusionReactorValidity.Valid;

    [DataField("torus")]
    public int MinimumTorus = 0;

    [DataField("magnet")]
    public int MinimumMagnet = 0;

    [DataField("alone")]
    public bool AllowedAlone = false;
}

[Flags, Serializable, NetSerializable]
public enum FusionReactorValidity : byte
{
    Valid = 0,
    InsufficientTorus = 1 << 0,
    InsufficientMagnet = 1 << 1,
    Alone = 1 << 2,
}