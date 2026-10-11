using System.Linq;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared._Starlight.Speech;

namespace Content.Server._Starlight.Speech.EntitySystems;

public sealed class BackwardsAccentSystem : RelayAccentSystem<BackwardsAccentComponent>
{
    public string Accentuate(string message)
    {
        var arr = message.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }

    protected override SpeechMessage AccentuateInternal(EntityUid uid, BackwardsAccentComponent component, SpeechMessage message)
    {
        message.Text = Accentuate(message.Text);
        message.Tts = string.Join(' ', (message.Tts ?? message.Text).Split(' ').Reverse());
        return message;
    }
}
