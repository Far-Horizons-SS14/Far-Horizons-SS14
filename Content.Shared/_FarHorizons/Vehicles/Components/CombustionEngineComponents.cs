using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.CombustionEngine;

[RegisterComponent]
public sealed partial class CombustionEngineComponent : Component
{
    /// <summary>
    /// ReagentID for what solution to use as fuel.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> Fuel = "WeldingFuel";
}
