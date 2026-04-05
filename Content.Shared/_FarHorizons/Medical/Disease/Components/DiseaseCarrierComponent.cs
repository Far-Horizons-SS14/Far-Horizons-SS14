using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;
using Content.Shared._FarHorizons.Medical.Disease.Systems;

namespace Content.Shared._FarHorizons.Medical.Disease.Components;

/// <summary>
/// Networked component storing active diseases and immunity tokens.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DiseaseCarrierComponent : Component
{
    /// <summary>
    /// Active diseases and their current stage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<DiseaseData, StageData> ActiveDiseases = [];

    /// <summary>
    /// Optional incubation end times per disease.
    /// Before this time, disease won't spread or show symptoms.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<DiseaseData, TimeSpan> IncubatingUntil = [];

    /// <summary>
    /// Delay between disease processing ticks.
    /// </summary>
    [DataField]
    public TimeSpan TickDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Time when the next disease processing tick occurs.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextTick;

    /// <summary>
    /// DiseaseData the entity is immune to and their immunity strength (0-1).
    /// Value represents the probability to block infection attempts for that disease.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<DiseaseData, float> Immunity = [];

    /// <summary>
    /// Map of symptom prototype IDs to a suppression end time. Used to temporarily
    /// suppress (treat) symptoms without curing the underlying disease.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<DiseaseSymptomPrototype>, TimeSpan> SuppressedSymptoms = [];

    /// <summary>
    /// Prototype ID of the disease icon to display for HUDs.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public ProtoId<DiseaseIconPrototype> DiseaseIcon = string.Empty;
}
