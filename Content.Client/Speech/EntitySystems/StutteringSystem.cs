using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared._Starlight.Speech;

namespace Content.Client.Speech.EntitySystems;

public sealed partial class StutteringSystem : SharedStutteringSystem
{
    protected override SpeechMessage AccentuateInternal(EntityUid uid, StutteringAccentComponent comp, SpeechMessage message)
    {
        return message;
    }
}
