using Content.Shared.Preferences;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Starlight.TextToSpeech;

[RegisterComponent, NetworkedComponent]
public sealed partial class TextToSpeechComponent : Component
{
    [DataField]
    public Symspeech? Symspeech { get; set; } // Far Horizons
}
