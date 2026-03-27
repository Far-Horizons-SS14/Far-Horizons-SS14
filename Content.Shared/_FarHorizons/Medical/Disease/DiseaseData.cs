using Content.Shared.Medical.Disease.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Disease.Systems;

[Serializable, NetSerializable]
public sealed class DiseaseData
{
    /// <summary>
    /// The prototype for this disease.
    /// </summary>
    [ViewVariables]
    public ProtoId<DiseasePrototype> Id;

    /// <summary>
    /// Randomized name for the strain of the disease.
    /// </summary>
    [ViewVariables]
    public string StrainName = string.Empty;
}

[Serializable, NetSerializable]
public sealed class StageData
{
    /// <summary>
    /// The stage for the disease
    /// </summary>
    [ViewVariables]
    public int Stage = 0;

    /// <summary>
    /// The time until the disease attempts spreading.
    /// </summary>
    [ViewVariables]
    public TimeSpan MinStageUntil;
    
    /// <summary>
    /// The time until the disease attempts forceful advance
    /// </summary>
    [ViewVariables]
    public TimeSpan MaxStageUntil;
}