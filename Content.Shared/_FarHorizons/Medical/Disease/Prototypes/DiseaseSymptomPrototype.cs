using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared._FarHorizons.Medical.Disease.Effects;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Medical.Disease.Prototypes;

/// <summary>
/// Describes information about a specific disease symptom.
/// </summary>
[Prototype]
public sealed partial class DiseaseSymptomPrototype : IPrototype, ISerializationHooks
{
    /// <summary>
    /// ID of the symptom.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Displayed name of the symptom.
    /// </summary>
    [DataField]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// What level is this symptom? For the sake of mutations. Higher level = Higher Rarity
    /// </summary>
    [DataField]
    public int Tier { get; private set; } = 1;

    /// <summary>
    /// The stats for this symptom this are added for the total stats of the disease.
    /// </summary>
    [DataField]
    public DiseaseStats Stats { get; private set; } = default!;

    /// <summary>
    /// Behavior variants configured by name. Each entry is a symptom effect with its own parameters.
    /// </summary>
    [DataField]
    public List<SymptomBehavior> Behaviors { get; private set; } = [];

    /// <summary>
    /// If true, only a single randomly selected behavior from <see cref="Behaviors"/> will run when the symptom triggers.
    /// </summary>
    [DataField]
    public bool SingleBehavior { get; private set; }

    /// <summary>
    /// If true, this symptom will only trigger on living carriers. If the carrier is dead the symptom is skipped.
    /// </summary>
    [DataField]
    public bool OnlyAlive { get; private set; }

    /// <summary>
    /// Configuration for symptom-driven airborne burst.
    /// </summary>
    [DataField]
    public SymptomAirborneBurst AirborneBurst { get; private set; } = new();

    /// <summary>
    /// How long (seconds) a successful symptom-level cure should suppress this symptom.
    /// If zero, symptom-level cures do not suppress.
    /// </summary>
    [DataField]
    public float CureDuration { get; private set; }

    /// <summary>
    /// Optional cure steps specific to this symptom. These are attempted by the cure system and, on success,
    /// suppress this symptom for <see cref="CureDuration"/> instead of curing the disease.
    /// </summary>
    [DataField]
    public List<CureStep> CureSteps { get; private set; } = [];

    void ISerializationHooks.AfterDeserialization()
    {
        for (var i = 0; i < Behaviors.Count; i++)
            Behaviors[i].Index = i;
    }
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class SymptomAirborneBurst
{
    /// <summary>
    /// Multiplier to disease airborne range for this burst.
    /// </summary>
    [DataField]
    public float RangeMultiplier { get; private set; } = 0.0f;

    /// <summary>
    /// Multiplier to disease airborne infection chance for this burst.
    /// </summary>
    [DataField]
    public float ChanceMultiplier { get; private set; } = 0.0f;
}

/// <summary>
/// Base class for symptom behavior.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public abstract partial class SymptomBehavior
{
    [ViewVariables]
    public int Index { get; internal set; }

    /// <summary>
    /// Conditions needed to be met before trigger symptom
    /// </summary>
    [DataField]
    public ISymptomCondition[] Conditions { get; private set; } = [];

    /// <summary>
    /// Probability per tick to trigger behavior when eligible (0-1).
    /// </summary>
    [DataField]
    public float Probability { get; set; } = 0.02f;

    /// <summary>
    /// Called when the symptom is triggered on the carrier.
    /// </summary>
    public virtual bool TryTriggerSymptom(Entity<DiseaseCarrierComponent> entity, DiseaseData disease, StageData stage, DiseaseSymptomPrototype symptom) => true;
}
