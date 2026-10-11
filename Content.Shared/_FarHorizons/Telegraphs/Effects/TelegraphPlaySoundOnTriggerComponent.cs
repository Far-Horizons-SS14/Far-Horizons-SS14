using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Telegraphs.Effects;

[RegisterComponent]
public sealed partial class TelegraphPlaySoundOnTriggerComponent : Component
{
    [DataField(required: true)] public SoundSpecifier Sound;
}