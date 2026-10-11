using System.Numerics;
using Content.Shared.Metabolism;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Medical.Disease.Prototypes;

/// <summary>
/// Describes information about a preset disease.
/// </summary>
[Prototype]
public sealed partial class DiseasePrototype : IPrototype
{
    /// <summary>
    /// ID of the disease.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Displayed name of the disease.
    /// </summary>
    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Displayed description of the disease.
    /// </summary>
    [DataField("desc", required: true)]
    public string Description { get; private set; } = default!;

    /// <summary>
    /// Name of the strain
    /// </summary>
    [DataField]
    public string StrainName = string.Empty;

    /// <summary>
    /// Id of the strain
    /// </summary>
    [DataField]
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
    public HashSet<ProtoId<MetabolizerTypePrototype>>? MetabolizerTypes;

    /// <summary>
    /// Symptoms for this disease.
    /// </summary>
    [DataField]
    public List<SymptomEntry> Symptoms { get; private set; } = new List<SymptomEntry>();

    /// <summary>
    /// Optional list of cure steps for the disease. Each entry is a specific cure action.
    /// </summary>
    [DataField]
    public List<CureStep> CureSteps { get; private set; } = new List<CureStep>();

    /// <summary>
    /// The Stealth level of the disease. Handled by Stats.
    /// </summary>
    [DataField]
    public DiseaseStealthFlags Stealth { get; set; } = DiseaseStealthFlags.None;

    /// <summary>
    /// Spread vectors for this disease. Handled by Stats.
    /// </summary>
    [DataField]
    public DiseaseSpreadPath SpreadPath { get; set; } = DiseaseSpreadPath.NonContagious;

    /// <summary>
    /// Handles the two thresholds for the disease timer so disease don't advance at the same tick.
    /// </summary>
    [DataField]
    public Vector2 DiseaseTimerThresholds { get; set; } = new(0.70f, 1.3f);

    /// <summary>
    /// Default immunity strength granted after curing this disease (0-1).
    /// </summary>
    [DataField]
    public float PostCureImmunity { get; set; } = 1.0f;

    /// <summary>
    /// Optional incubation time in seconds before symptoms/spread begin after infection.
    /// </summary>
    [DataField]
    public float IncubationSeconds { get; set; }

    /// <summary>
    /// Per-disease permeability multiplier (0-1) applied to PPE/internals effectiveness.
    /// Values > 1 reduce protection; values < 1 increase protection.
    /// </summary>
    [DataField]
    public float PermeabilityMod { get; set; } = 1.0f;

    /// <summary>
    /// Base per-contact infection probability for this disease (0-1). Used when two entities make contact.
    /// </summary>
    [DataField]
    public float ContactInfect { get; set; } = 0.025f;

    /// <summary>
    /// Amount of residue intensity deposited when a carrier with this disease contacts a surface.
    /// Expressed as (0-1) fraction added to per-disease residue intensity.
    /// </summary>
    [DataField]
    public float ContactDeposit { get; set; } = 0.2f;

    /// <summary>
    /// Base per-target airborne infection probability (0-1) before PPE adjustments.
    /// </summary>
    [DataField]
    public float AirborneInfect { get; set; } = 0.025f;

    /// <summary>
    /// Airborne infection radius in world units, used when <see cref="SpreadPath"/> contains Airborne.
    /// </summary>
    [DataField]
    public float AirborneRange { get; set; } = 3f;

    /// <summary>
    /// Disease icon prototype to show on HUDs.
    /// </summary>
    [DataField]
    public ProtoId<DiseaseIconPrototype>? IconDisease { get; private set; } = "DiseaseIconIll";
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class SymptomEntry
{
    /// <summary>
    /// Symptom prototype ID to trigger.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DiseaseSymptomPrototype> Symptom { get; private set; }
    
    /// <summary>
    /// Per stage level probability overwrites the symptom level probability if > -1.
    /// </summary>
    [DataField]
    public Dictionary<int, float> Probability { get; private set; } = new()
    {
        { 0, -1f } // Int: Stage, Float: Chance
    };
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class DiseaseStats
{
    [DataField]
    public int Stealth { get; set; } = 0;

    [DataField]
    public int Resistance { get; set; } = 0;

    [DataField]
    public int Speed { get; set; } = 0;

    [DataField]
    public int Transmittable { get; set; } = 0;
}