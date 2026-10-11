using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Telegraphs.Effects;

[RegisterComponent]
public sealed partial class TelegraphFollowupAttackOnTriggerComponent : Component
{
    [DataField(required: true)] public EntProtoId Entity;
}