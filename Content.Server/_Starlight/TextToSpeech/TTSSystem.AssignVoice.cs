using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Content.Shared.Starlight.TextToSpeech;
using Robust.Shared.Prototypes;

namespace Content.Server.Starlight.TTS;

public sealed partial class TTSSystem
{
    // Far Horizons edit start - change to symspeech
    private Symspeech GetOrAssignVoice(EntityUid uid, TextToSpeechComponent? component = default, Symspeech? fallbackVoice = null)
    {
        fallbackVoice ??= _defaultVoice;
        if (component is null && !TryComp(uid, out component))
            return fallbackVoice;

        if (component.Symspeech is { } symspeech
            && _prototypeManager.TryIndex(symspeech.Voice, out VoicePrototype? proto))
            return symspeech;

        var isHumanoid = false;

        if (TryComp<HumanoidProfileComponent>(uid, out var humanoidAppearanceComponent)
            && humanoidAppearanceComponent?.Sex is Sex sex1)
        {
            isHumanoid = true;
        }

        if (TryComp<MindContainerComponent>(uid, out var mindContainer)
            && mindContainer.HasMind
            && TryComp<MindComponent>(mindContainer.Mind, out var mind))
        {
            if (isHumanoid)
            {
                if (mind.Symspeech?.Voice is { } mindVoiceId && _prototypeManager.TryIndex(mindVoiceId, out VoicePrototype? mindVoice))
                {
                    component.Symspeech?.Voice = mindVoiceId;
                    return mind.Symspeech;
                }
            }
            else
            {
                if (mind.SiliconSymspeech?.Voice is { } mindVoiceId && _prototypeManager.TryIndex(mindVoiceId, out VoicePrototype? mindVoice))
                {
                    component.Symspeech?.Voice = mindVoiceId;
                    return mind.SiliconSymspeech;
                }
            }
        }

        if (!_prototypeManager.TryGetInstances<VoicePrototype>(out var voices))
            return fallbackVoice;

        return AssignRandomVoice(voices.ToArray());

        Symspeech AssignRandomVoice(KeyValuePair<string, VoicePrototype>[] voicePrototypes)
        {
            if (voicePrototypes.Length == 0)
                return fallbackVoice;

            var index = _rng.Next(voicePrototypes.Length);
            var prototype = voicePrototypes[index];
            component.Symspeech?.Voice = prototype.Value.ID;
            return new Symspeech(
                prototype.Value.ID,
                prototype.Value.DefaultPitch,
                prototype.Value.DefaultSpeed,
                prototype.Value.DefaultPause,
                prototype.Value.DefaultPolyphony,
                prototype.Value.DefaultVolume
                );
        }
    }
    // Far Horizons edit end
}
