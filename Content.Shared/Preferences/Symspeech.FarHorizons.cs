using Content.Shared.Starlight.TextToSpeech;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class Symspeech
{
    [DataField] public ProtoId<VoicePrototype> Voice;

    [DataField] public int Pitch;

    [DataField] public float Speed;

    [DataField] public float Pause;

    [DataField] public int Polyphony;

    [DataField] public float Volume;

    public Symspeech(ProtoId<VoicePrototype> voice, int pitch, float speed, float pause, int polyphony, float volume)
    {
        Voice = voice;
        Pitch = pitch;
        Speed = speed;
        Pause = pause;
        Polyphony = polyphony;
        Volume = volume;
    }
}