namespace Content.Shared._FarHorizons.Audio;

[RegisterComponent]
public sealed partial class StructureOcclusionComponent : Component
{
    /// <summary>
    /// How much this entity contributes to occlusion when a sound ray passes through it.
    /// 1.0 = baseline wall. Higher = more muffling (reinforced walls, plasteel walls, etc).
    /// </summary>
    [DataField]
    public float OcclusionAmount = 1f;

    /// <summary>
    /// Does the structure's occlusion still work if the object is open?
    /// </summary>
    [DataField]
    public bool DoesOcclusionWorkWhenOpen = false;  

    /// <summary>
    /// An additive amount to OcclusionAmount for when a structure is welded shut.
    /// </summary>
    [DataField]
    public float WeldedOcclusionModifier = 2.0f;

}