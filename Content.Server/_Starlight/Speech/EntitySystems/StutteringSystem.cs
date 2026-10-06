using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffectNew;
using Content.Shared._Starlight.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed partial class StutteringSystem : SharedStutteringSystem
    {
        [Dependency] private IRobustRandom _random = default!;

        // Regex of characters to stutter.
        [GeneratedRegex(@"[b-df-hj-np-tv-wxyz]", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
        private static partial Regex Stutter();

        public override void DoStutter(EntityUid uid, TimeSpan time, bool refresh)
        {
            if (refresh)
                Status.TryUpdateStatusEffectDuration(uid, Stuttering, time);
            else
                Status.TryAddStatusEffectDuration(uid, Stuttering, time);
        }

        public override void DoRemoveStutterTime(EntityUid uid, TimeSpan timeRemoved)
            => Status.TryAddTime(uid, Stuttering, -timeRemoved);

        public override void DoRemoveStutter(EntityUid uid)
            => Status.TryRemoveStatusEffect(uid, Stuttering);

        protected override SpeechMessage AccentuateInternal(EntityUid uid, StutteringAccentComponent component, SpeechMessage message)
        {
            message.Text = Accentuate(message.Text, component);
            return message;
        }

        public string Accentuate(string message, StutteringAccentComponent component)
        {
            var length = message.Length;

            var finalMessage = new StringBuilder();

            string newLetter;

            for (var i = 0; i < length; i++)
            {
                newLetter = message[i].ToString();
                if (Stutter().IsMatch(newLetter) && _random.Prob(component.MatchRandomProb))
                {
                    newLetter = _random.Prob(component.FourRandomProb) ? $"{newLetter}-{newLetter}-{newLetter}-{newLetter}"
                            : _random.Prob(component.ThreeRandomProb) ? $"{newLetter}-{newLetter}-{newLetter}"
                            : _random.Prob(component.CutRandomProb) ? "" 
                            : $"{newLetter}-{newLetter}";
                }

                finalMessage.Append(newLetter);
            }

            return finalMessage.ToString();
        }
    }
}
