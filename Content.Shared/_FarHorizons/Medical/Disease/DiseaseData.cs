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

    [ViewVariables]
    public TimeSpan MinStageUntil;

    [ViewVariables]
    public TimeSpan MaxStageUntil;
}