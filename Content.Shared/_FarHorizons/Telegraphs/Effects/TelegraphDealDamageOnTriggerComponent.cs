using Content.Shared.Damage;

namespace Content.Shared._FarHorizons.Telegraphs.Effects;

[RegisterComponent]
public sealed partial class TelegraphDealDamageOnTriggerComponent : Component
{
    [DataField(required: true)] public DamageSpecifier Damage;
    [DataField] public bool IgnoreResistances;
    [DataField] public bool AllLimbs = true;
}