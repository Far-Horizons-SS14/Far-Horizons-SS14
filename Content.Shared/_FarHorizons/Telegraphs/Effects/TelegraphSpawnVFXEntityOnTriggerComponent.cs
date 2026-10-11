using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Telegraphs.Effects;

[RegisterComponent]
public sealed partial class TelegraphSpawnVFXEntityOnTriggerComponent : Component
{
    [DataField(required: true)] public EntProtoId Entity;
    [DataField] public bool Single;
}