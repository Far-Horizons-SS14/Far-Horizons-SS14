using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Fluids.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FluidFootprintSourceComponent : Component
{
    [DataField(required: true)] public float MinUnitsForFootPrint = default!;
    [DataField(required: true)] public int MinFootprints = default!;
    [DataField(required: true)] public float BonusPerUnit = default!;
    [DataField(required: true)] public TimeSpan StopAfter = default!;
    [DataField] public float TakeSolutionUnits = 1f;
    [DataField] public TimeSpan DraggingFootprintRate = TimeSpan.FromSeconds(0.1f);
    [DataField(required: true)] public float DraggingStepSpacing = default!;
    [DataField(required: true)] public ProtoId<FootprintTypePrototype> DraggingFootprint = default!;
    [DataField] public float DraggingFootprintSize = 1.0f;
}