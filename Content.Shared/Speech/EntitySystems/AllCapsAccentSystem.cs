using Content.Shared._Starlight.Speech;

namespace Content.Shared.Speech.EntitySystems;

/// <summary>
/// Applies the all-caps accent to speech and relayed speech status effect events.
/// </summary>
public sealed class AllCapsAccentSystem : RelayAccentSystem<Components.AllCapsAccentComponent>
{
    // Far Horizons - we changed this from string to SpeechMessage
    protected override SpeechMessage AccentuateInternal(EntityUid uid, Components.AllCapsAccentComponent comp, SpeechMessage message)
        => new SpeechMessage() { Text = message.Text.ToUpperInvariant(), Tts = message.Tts, Modifier = message.Modifier };
}
