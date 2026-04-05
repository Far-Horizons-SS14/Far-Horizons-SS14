using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Medical.Disease.Components;

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

    /// <summary>
    /// DiseaseData the entity is immune to and their immunity strength (0-1).
    /// Value represents the probability to block infection attempts for that disease.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<DiseaseData, float> Immunity = [];

}
