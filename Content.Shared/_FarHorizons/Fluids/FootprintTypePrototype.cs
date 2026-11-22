using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.Fluids;

[Prototype]
public sealed partial class FootprintTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)] public ResPath RsiPath = default!;
    [DataField(required: true)] public string RsiState = default!;
    [DataField(required: true)] public bool Alternating = default!;
}