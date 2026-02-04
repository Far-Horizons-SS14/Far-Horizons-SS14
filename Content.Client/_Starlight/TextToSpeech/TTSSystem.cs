using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Content.Client._Starlight.Radio.Systems;
using Content.Client._Starlight.TextToSpeech;
using Content.Shared.Starlight.CCVar;
using Content.Shared.Starlight.TextToSpeech;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Starlight.TTS;

/// <summary>
/// Plays TTS audio
/// </summary>
public sealed class TextToSpeechSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedAudioSystem _sharedAudio = default!;
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly RadioChimeSystem _chime = default!;

    private readonly ConcurrentQueue<(Queue<byte[]> data, SoundSpecifier? specifier, float volume)> _ttsQueue = [];
    private ISawmill _sawmill = default!;
    private readonly MemoryContentRoot _contentRoot = new();
    private (EntityUid Entity, AudioComponent Component)? _currentPlaying;

    private float _volume;
    private float _radioVolume;
    private float _announceVolume;
    private bool _ttsQueueEnabled;

    public void ClearQueue()
    {
        _ttsQueue.Clear();

        if (_currentPlaying.HasValue)
        {
            var (entity, _) = _currentPlaying.Value;
            if (!Deleted(entity))
                QueueDel(entity);
            _currentPlaying = null;
        }
    }

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(StarlightCCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(StarlightCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged, true);
        _cfg.OnValueChanged(StarlightCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);
        _cfg.OnValueChanged(StarlightCCVars.TTSRadioQueueEnabled, OnTtsRadioQueueChanged, true);
        _cfg.OnValueChanged(StarlightCCVars.TTSClientEnabled, OnTtsClientOptionChanged, true);
        SubscribeLocalEvent<TTSStream>(OnTTSStream);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(StarlightCCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(StarlightCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged);
        _cfg.UnsubValueChanged(StarlightCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);
        _cfg.UnsubValueChanged(StarlightCCVars.TTSRadioQueueEnabled, OnTtsRadioQueueChanged);
        _cfg.UnsubValueChanged(StarlightCCVars.TTSClientEnabled, OnTtsClientOptionChanged);
        _contentRoot.Dispose();
    }

    public void RequestPreviewTts(string voiceId)
        => RaiseNetworkEvent(new PreviewTTSRequestEvent() { VoiceId = voiceId });

    private void OnTtsVolumeChanged(float volume)
        => _volume = volume;

    private void OnTtsRadioVolumeChanged(float volume)
        => _radioVolume = volume;

    private void OnTtsRadioQueueChanged(bool enabled)
        => _ttsQueueEnabled = enabled;

    private void OnTtsAnnounceVolumeChanged(float volume)
        => _announceVolume = volume;

    private void OnTtsClientOptionChanged(bool option)
        => RaiseNetworkEvent(new ClientOptionTTSEvent { Enabled = option });

    private void PlayQueue()
    {
        if (!_ttsQueue.TryDequeue(out var entry))
            return;

        var volume = SharedAudioSystem.GainToVolume(entry.volume);
        var finalParams = AudioParams.Default.WithVolume(volume);

        // adaptive pitch scaling
        switch (_ttsQueue.Count)
        {
            case int x when x < 2:
                break;
            case 2:
                finalParams = finalParams.WithPitchScale(1.01f);
                break;
            case 3:
                finalParams = finalParams.WithPitchScale(1.03f);
                break;
            case 4:
                finalParams = finalParams.WithPitchScale(1.09f);
                break;
            case 5:
                finalParams = finalParams.WithPitchScale(1.12f);
                break;
            case 6:
                finalParams = finalParams.WithPitchScale(1.21f);
                break;
            case 7:
                finalParams = finalParams.WithPitchScale(1.33f);
                break;
            case 8:
                finalParams = finalParams.WithPitchScale(1.54f);
                break;
            case 9:
                finalParams = finalParams.WithPitchScale(1.87f);
                break;
            default:
                return;
        }

        if (entry.specifier != null)
            _currentPlaying = _audio.PlayGlobal(_sharedAudio.ResolveSound(entry.specifier), EntityUid.Invalid, finalParams.AddVolume(-5f));
        _currentPlaying = PlayTTS(entry.data, null, finalParams);
    }

    private void OnTTSStream(TTSStream ev)
    {
        var volume = ev.Type switch
        {
            TTSType.Announcement => _announceVolume,
            TTSType.System => _announceVolume,
            TTSType.Radio => _radioVolume,
            TTSType.Mind => _radioVolume,
            TTSType.IG => _volume,
            _ => _volume
        };

        if (ev.Type == TTSType.Announcement || (ev.Type == TTSType.Radio && _ttsQueueEnabled))
        {
            _ttsQueue.Enqueue((ev.Data, !_chime.IsMuted ? ev.Chime : null, _radioVolume));
        }
        else
        {
            volume = SharedAudioSystem.GainToVolume(volume * ev.VolumeModifier);
            var audioParams = AudioParams.Default.WithVolume(volume);
            var entity = GetEntity(ev.SourceUid);

            if (!_chime.IsMuted && ev.Chime is SoundSpecifier chime)
            {
                var audio = _sharedAudio.ResolveSound(chime);
                var ent = _audio.PlayGlobal(audio, EntityUid.Invalid, audioParams.AddVolume(-3f));
                if (ent != null)
                {
                    var comp = EnsureComp<TTSAudioStreamComponent>(ent.Value.Entity);
                    comp.Data = ev.Data;
                    comp.EntityUid = ent.Value.Entity;
                    comp.SourceUid = entity;
                    comp.AudioParams = audioParams;
                    comp.AudioLength = _audio.GetAudioLength(audio);
                    _currentPlaying = ent;
                    return;
                }
            }
            _currentPlaying = PlayTTS(ev.Data, entity, audioParams);
        }
    }

    private (EntityUid Entity, AudioComponent Component)? PlayTTS(
        Queue<byte[]> data,
        EntityUid? sourceUid = null,
        AudioParams? audioParams = null,
        (EntityUid eid, AudioComponent audio, TTSAudioStreamComponent tts)? previous = null)
    {
        try
        {
            if (!data.TryDequeue(out var audioBytes))
                return null;

            if (audioBytes.Length < 10 || (sourceUid != null && sourceUid.Value.Id == 0))
                return null;

            var silencePadding = 1f;
            var @params = audioParams ?? AudioParams.Default;
            var audioStream = _audioManager.LoadAudioOggVorbis(new MemoryStream(audioBytes));

            if (previous is var (eid, audio, tts))
                silencePadding = Math.Clamp(1f - (float)(tts.AudioLength.TotalSeconds - audio.PlaybackPosition), 0f, 1f);

            _sawmill.Debug($"Play TTS chunk: {audioBytes.Length}, prependSilence: {silencePadding:F3}s");
            @params = @params.WithPlayOffset(silencePadding);
            var ent = sourceUid != null && sourceUid != _player.LocalEntity
                ? _audio.PlayEntity(audioStream, sourceUid.Value, null, @params)
                : _audio.PlayGlobal(audioStream, null, @params);

            if (ent != null)
            {
                var comp = EnsureComp<TTSAudioStreamComponent>(ent.Value.Entity);
                comp.Data = data;
                comp.EntityUid = ent.Value.Entity;
                comp.SourceUid = sourceUid;
                comp.AudioParams = audioParams;
                comp.AudioLength = audioStream.Length;

                if(_currentPlaying.HasValue && previous.HasValue && previous.Value.eid == ent.Value.Entity)
                   _currentPlaying = ent;
            }

            return ent;
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Error playing TTS audio: {ex.Message}", ex);
        }

        return null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toPlay = new List<(EntityUid eid, AudioComponent audio, TTSAudioStreamComponent tts)>();
        var query = EntityQueryEnumerator<TTSAudioStreamComponent, TimedDespawnComponent, AudioComponent>();

        while (query.MoveNext(out var uid, out var ttsComp, out var despawnComponent, out var audio))
        {
            if (ttsComp.Handled)
                continue;
            var timeRemaining = despawnComponent.Lifetime - SharedAudioSystem.AudioDespawnBuffer - 1f;

            if (timeRemaining < 0.066f)
                toPlay.Add((uid, audio, ttsComp));
        }

        foreach (var (eid, audio, tts) in toPlay)
        {
            if (PlayTTS(tts.Data, tts.SourceUid, tts.AudioParams, (eid, audio, tts)) is not null)
                tts.Handled = true;
        }

        if (_currentPlaying.HasValue)
        {
            var (entity, _) = _currentPlaying.Value;

            if (Deleted(entity))
                _currentPlaying = null;
            else
                return;
        }

        PlayQueue();
    }
}
