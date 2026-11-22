using Content.Shared.StatusEffectNew;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Content.Shared._Starlight.Speech;

namespace Content.Server._Starlight.Speech.EntitySystems;

public sealed partial class BarkAccentSystem : RelayAccentSystem<BarkAccentComponent>
{
    [Dependency] private IRobustRandom _random = default!;

    private static readonly IReadOnlyList<string> _barks = new List<string>{
        " Woof!", " WOOF", " wof-wof"
    }.AsReadOnly();

    private static readonly IReadOnlyDictionary<string, string> _specialWords = new Dictionary<string, string>()
    {
        { "ah", "arf" },
        { "Ah", "Arf" },
        { "oh", "oof" },
        { "Oh", "Oof" },
    };

    public SpeechMessage Accentuate(SpeechMessage message)
    {
        foreach (var (word, repl) in _specialWords)
        {
            message.Text = message.Text.Replace(word, repl);
            message.Tts = (message.Tts ?? message.Text).Replace(word, repl);
        }

        message.Text = message.Text.Replace("!", _random.Pick(_barks))
            .Replace("l", "r")
            .Replace("L", "R");

        message.Tts = (message.Tts ?? message.Text).Replace("!", " Woof!");

        return message;
    }

    protected override SpeechMessage AccentuateInternal(EntityUid uid, BarkAccentComponent component, SpeechMessage message)
        => Accentuate(message);
}
