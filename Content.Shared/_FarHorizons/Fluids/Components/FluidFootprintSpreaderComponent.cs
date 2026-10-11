using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Fluids.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FluidFootprintSpreaderComponent : Component
{
    [DataField] public TimeSpan FootprintRate = TimeSpan.FromSeconds(0.1f);
    [DataField(required: true)] public ProtoId<FootprintTypePrototype> Footprint = default!;
    [DataField] public float FootprintSize = 1f;
    [DataField(required: true)] public float StepSpacing = default!;
    [DataField(required: true)] public float LateralOffset = default!;
}