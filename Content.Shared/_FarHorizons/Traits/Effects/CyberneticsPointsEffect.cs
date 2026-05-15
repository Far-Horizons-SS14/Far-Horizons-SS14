using Content.Shared._Starlight.Traits.Effects;

namespace Content.Shared._FarHorizons.Traits.Effects;

/// <summary>
/// Effect that lets player put more special cybernetics on their character
/// </summary>
public sealed partial class CyberneticsPointsEffect : BaseTraitEffect
{
    [DataField(required: true)] public int Change;

    public override void Apply(TraitEffectContext ctx) { }
}