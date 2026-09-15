using System.Numerics;
using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Content.Shared.Metabolism;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Medical.Disease.Systems;

/// <summary>
/// Mutable Data of the disease.
/// </summary>
[Serializable, NetSerializable]
[DataRecord]
public partial record struct DiseaseData
{
    /// <summary>
    /// The prototype for this disease.
    /// </summary>
    [ViewVariables]
    public ProtoId<DiseasePrototype> Id;

    /// <summary>
    /// Displayed name of the disease.
    /// </summary>
    [DataField(required: true)]
    public string Name = default!;

    /// <summary>
    /// Displayed description of the disease.
    /// </summary>
    [DataField(required: true)]
    public string Description = default!;

    /// <summary>
    /// Randomized name for the strain of the disease.
    /// </summary>
    [DataField]
    public string StrainName = string.Empty;

    /// <summary>
    /// Preset id of the strain
    /// </summary>
    [IdDataField]
    public string? StrainId = string.Empty;

    /// <summary>
    /// The disease vector is what controls the disease's base stats and timers.
    /// </summary>
    [DataField]
    public ProtoId<DiseaseVectorPrototype> Vector { get; set; } = default!;

    /// <summary>
    /// Optional list of metabolizers that are affected by this disease.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MetabolizerTypePrototype>>? MetabolizerTypes { get; set; }

    /// <summary>
    /// Symptoms for this disease.
    /// </summary>
    [DataField]
    public List<SymptomEntry> Symptoms { get; set; } = new List<SymptomEntry>();

    /// <summary>
    /// Optional list of cure steps for the disease. Each entry is a specific cure action.
    /// </summary>
    [DataField]
    public List<CureStep> CureSteps { get; set; } = new List<CureStep>();

    /// <summary>
    /// The total stats from summing all the symptom stats.
    /// </summary>
    [ViewVariables]
    public DiseaseStats Stats { get; set; } = default!;

    /// <summary>
    /// The Stealth level of the disease. Handled by Stats.
    /// </summary>
    [ViewVariables]
    public DiseaseStealthFlags Stealth { get; set; } = DiseaseStealthFlags.None;

    /// <summary>
    /// Spread vectors for this disease. Handled by Stats.
    /// </summary>
    [ViewVariables]
    public DiseaseSpreadPath SpreadPath = DiseaseSpreadPath.NonContagious;

    /// <summary>
    /// Handles the two thresholds for the disease timer so disease don't advance at the same tick.
    /// </summary>
    [ViewVariables]
    public Vector2 DiseaseTimerThresholds = new(0.70f, 1.3f);

    /// <summary>
    /// Default immunity strength granted after curing this disease (0-1).
    /// </summary>
    [DataField]
    public float PostCureImmunity = 1.0f;

    /// <summary>
    /// Optional incubation time in seconds before symptoms/spread begin after infection.
    /// </summary>
    [DataField]
    public float IncubationSeconds;

    /// <summary>
    /// Per-disease permeability multiplier (0-1) applied to PPE/internals effectiveness.
    /// Values > 1 reduce protection; values < 1 increase protection.
    /// </summary>
    [DataField]
    public float PermeabilityMod = 1.0f;

    /// <summary>
    /// Base per-contact infection probability for this disease (0-1). Used when two entities make contact.
    /// </summary>
    [DataField]
    public float ContactInfect = 0.025f;

    /// <summary>
    /// Amount of residue intensity deposited when a carrier with this disease contacts a surface.
    /// Expressed as (0-1) fraction added to per-disease residue intensity.
    /// </summary>
    [DataField]
    public float ContactDeposit = 0.2f;

    /// <summary>
    /// Base per-target airborne infection probability (0-1) before PPE adjustments.
    /// </summary>
    [DataField]
    public float AirborneInfect = 0.025f;

    /// <summary>
    /// Airborne infection radius in world units, used when <see cref="SpreadPath"/> contains Airborne.
    /// </summary>
    [DataField]
    public float AirborneRange = 3f;

    /// <summary>
    /// Disease icon prototype to show on HUDs.
    /// </summary>
    [DataField]
    public ProtoId<DiseaseIconPrototype>? IconDisease { get; set; } = "DiseaseIconIll";
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
    /// The time the disease advances to its next stage.
    /// </summary>
    [ViewVariables]
    public TimeSpan AdvanceStageAt;
}