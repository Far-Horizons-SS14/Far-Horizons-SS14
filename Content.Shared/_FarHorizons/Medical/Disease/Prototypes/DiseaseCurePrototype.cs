using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Disease.Prototypes;

/// <summary>
/// Prototype for the cures and its tier.
/// </summary>
[Prototype("Cure")]
public sealed partial class CurePrototype : IPrototype
{
    /// <summary>
    /// 
    /// </summary>
    [IdDataField] 
    public string ID { get; private set; } = null!;

    /// <summary>
    /// 
    /// </summary>
    [DataField(required:true)]
    public int Tier { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    [DataField(required:true)]
    public CureStep? CureStep { get; set; }
}
