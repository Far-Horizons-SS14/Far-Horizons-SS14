using System.Text;
using Content.Server.Speech.EntitySystems;
using Content.Shared._FarHorizons.Mobs;
using Content.Shared.Speech;

namespace Content.Server._FarHorizons.Mobs;

public sealed class ActiveCritSystem : SharedActiveCritSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveCritComponent, AccentGetEvent>(OnAccent, after: [typeof(ReplacementAccentSystem)]);
    }

    private void OnAccent(Entity<ActiveCritComponent> ent, ref AccentGetEvent args)
    {
        if (_mobState.IsCritical(ent.Owner))
            args.Message.Text = DistortSpeech(args.Message.Text, ent.Comp.SpeechDistortionStrength);
    }

    public string DistortSpeech(string message, float strength)
    {
        if (string.IsNullOrWhiteSpace(message) || strength <= 0f) 
            return message;

        strength = Math.Clamp(strength, 0f, 1f);
        
        var result = new StringBuilder(message.Length * 2);
        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (Random.Shared.NextDouble() < strength * 0.5f)
                result.Append(_struggleSounds[Random.Shared.Next(_struggleSounds.Length)]);

            for (var i = 0; i < word.Length; i++)
            {
                var c = word[i];
                var isLetter = char.IsLetter(c);

                if (isLetter)
                {
                    if (Random.Shared.NextDouble() < strength * 0.45f)
                    {
                        if (Random.Shared.NextDouble() < 0.5f)
                            result.Append('\''); 
                            
                        continue;
                    }

                    var stutterChance = i == 0 ? strength * 0.4f : strength * 0.25f;
                    if (Random.Shared.NextDouble() < stutterChance)
                        result.Append(char.ToLower(c)).Append('-');

                    if (char.IsUpper(c) && Random.Shared.NextDouble() < strength * 0.6f)
                        c = char.ToLower(c);
                }

                result.Append(c);
            }
            
            result.Append(' ');
        }

        return result.ToString().Trim();
    }

    private static readonly string[] _struggleSounds =
    [
        "...", "..", " - ", " hhh- ", " kh... ", " ngh- ", " hgh... "
    ];
}