using Content.Shared.Medical.Disease.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Disease.Components;

/// <summary>
/// Represents a collected disease sample, storing disease prototype IDs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DiseaseSampleComponent : Component
{
    /// <summary>
    /// Determines whether there is a sample on the swab.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasSample;

    /// <summary>
    /// Display name of the sampled subject at the time of sampling.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? SubjectName;

    /// <summary>
    /// DNA string of the sampled subject at the time of sampling.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? SubjectDNA;

    /// <summary>
    /// Disease + Stage Data all in one for diagnosis
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<DiseaseData, StageData> DiseasesData = [];
}
