using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Fluids.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveFluidFootprintSpreaderComponent : Component
{
    [ViewVariables] public int RemainingFootprints = 0;
    [ViewVariables] public MapCoordinates? LastPosition;
    [ViewVariables] public TimeSpan NextStep = TimeSpan.Zero;
    [ViewVariables] public TimeSpan StopAt = TimeSpan.Zero;
    [ViewVariables] public Color Color = Color.White;
    [ViewVariables] public bool Left = true;
    [ViewVariables] public float Opacity = 1f;
    [ViewVariables] public float OpacityStep;
    [ViewVariables] public TimeSpan? FootprintRate;
    [ViewVariables] public float? StepSpacing;
    [ViewVariables] public ProtoId<FootprintTypePrototype>? Footprint;
    [ViewVariables] public float? LateralOffset;
    [ViewVariables] public float FootprintSize = 1f;
}
