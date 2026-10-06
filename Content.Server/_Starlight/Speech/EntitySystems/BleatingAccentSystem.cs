using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared._Starlight.Speech;

namespace Content.Server._Starlight.Speech.EntitySystems;

public sealed partial class BleatingAccentSystem : RelayAccentSystem<BleatingAccentComponent>
{
    [GeneratedRegex("([mbdlpwhrkcnytfo])([aiu])", RegexOptions.IgnoreCase)]
    private static partial Regex BleatRegex();

    protected override SpeechMessage AccentuateInternal(EntityUid uid, BleatingAccentComponent component, SpeechMessage message)
        // Only modify displayed text, TTS stays normal
        => new SpeechMessage() { Text = Accentuate(message.Text), Tts = message.Tts, Modifier = message.Modifier };

    public static string Accentuate(string message) =>
         // Repeats the vowel in certain consonant-vowel pairs
         // So you taaaalk liiiike thiiiis
         BleatRegex().Replace(message, "$1$2$2$2$2");
}
