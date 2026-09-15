using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Medical.Disease.Prototypes;

/// <summary>
/// Base class for cure step variants.
/// </summary>
[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class CureStep
{
    /// <summary>
    /// If true, a successful cure step lowers the current disease stage by 1 instead of curing entirely.
    /// </summary>
    [DataField]
    public bool LowerStage { get; private set; }

    /// <summary>
    /// Per-tick probability (0-1) to attempt this cure step.
    /// </summary>
    [DataField]
    public float CureChance { get; private set; } = 1.0f;

    /// <summary>
    /// Attempts to execute this cure step on the given entity.
    /// </summary>
    public virtual bool OnCure(EntityUid uid, DiseaseData disease) 
        => false;

    /// <summary>
    /// Returns one or more localized lines describing this cure step for diagnoser reports.
    /// </summary>
    public virtual IEnumerable<string> BuildDiagnoserLines(IPrototypeManager prototypes)
    {
        yield break;
    }
}
