using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Fusion.Reactions;

[Prototype]
public sealed partial class FusionConversionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Gas Gas { get; private set; }

    /// <summary>
    /// It's a List (FusionAtom, double) at heart
    /// </summary>
    [DataField]
    public Dictionary<Vector2i, double> Products { get; private set; } = [];
}