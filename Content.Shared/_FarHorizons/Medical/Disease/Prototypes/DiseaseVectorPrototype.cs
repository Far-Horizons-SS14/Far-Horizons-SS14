using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Medical.Disease.Prototypes;

/// <summary>
/// Prototype for disease vectors. They handle the base stats for a disease and its timers.
/// </summary>
[Prototype("Vector")]
public sealed partial class DiseaseVectorPrototype : IPrototype
{
    /// <summary>
    /// The ID for this disease vector
    /// </summary>
    [IdDataField] 
    public string ID { get; private set; } = null!;

    /// <summary>
    /// The base stats for this type of disease vector.
    /// </summary>
    [DataField]
    public DiseaseStats Stats { get; set; } = default!;

    /// <summary>
    /// The base stats for this type of disease vector.
    /// </summary>
    [DataField]
    public DiseaseTimers Timers { get; set; } = default!;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class DiseaseTimers
{
    [DataField]
    public int Stage01 { get; set; } = 240;

    [DataField]
    public int Stage12 { get; set; } = 180;

    [DataField]
    public int Stage23 { get; set; } = 120;

    [DataField]
    public int Stage34 { get; set; } = 60;

    public int Count => 4;

    public int this[int index] => index switch
    {
        0 => Stage01,
        1 => Stage12,
        2 => Stage23,
        3 => Stage34,
        _ => throw new ArgumentOutOfRangeException(nameof(index), $"Disease only has stages 0-3, got {index}")
    };
}