using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared._Starlight.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class ChavAccentSystem : RelayAccentSystem<ChavAccentComponent>
{
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    protected override SpeechMessage AccentuateInternal(EntityUid uid, ChavAccentComponent component, SpeechMessage message)
    {
        message = _replacement.ApplyReplacements(message, "chav");

        message.Text = message.Text
            .Replace("th", "ff")
            .Replace("Th", "Ff")
            .Replace("TH", "FF");

        return message;
    }
}
