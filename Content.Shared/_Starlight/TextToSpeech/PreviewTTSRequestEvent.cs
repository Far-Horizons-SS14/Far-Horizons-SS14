using Content.Shared.Preferences;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.TextToSpeech;

[Serializable, NetSerializable]
public sealed class PreviewTTSRequestEvent : EntityEventArgs
{
    public Symspeech Symspeech { get; set; } = null!;
}
