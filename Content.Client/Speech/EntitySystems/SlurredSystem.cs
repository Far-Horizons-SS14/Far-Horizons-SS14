using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared._Starlight.Speech;

namespace Content.Client.Speech.EntitySystems;

public sealed partial class SlurredSystem : SharedSlurredSystem
{
    protected override SpeechMessage AccentuateInternal(EntityUid uid, SlurredAccentComponent comp, SpeechMessage message) // FH string -> SpeechMessage
    {
        return message;
    }
}
