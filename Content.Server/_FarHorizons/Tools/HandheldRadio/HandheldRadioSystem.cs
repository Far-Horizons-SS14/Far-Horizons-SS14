using Robust.Server.GameObjects;
using Content.Shared._FarHorizons.Tools.HandheldRadio;
using Content.Shared.Interaction.Events;
using Content.Shared.Examine;
using Content.Shared.Speech;
using Content.Server.Interaction;
using Content.Shared.Speech.Components;
using Content.Shared.Chat;
using Content.Server._Starlight.Language;
using Content.Server.Chat.Systems;
using Content.Shared.Verbs;
using Content.Server.Popups;
using Content.Shared._Starlight.Language;
using Content.Shared._Starlight.Language.Components;

namespace Content.Server._FarHorizons.Tools.HandheldRadio;

public sealed class HandheldRadioSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    private readonly Dictionary<float, HashSet<Entity<HandheldRadioComponent>>> _frequencyCache = [];

    private readonly HashSet<(float, EntityUid, string)> _recentlySent = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandheldRadioComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HandheldRadioComponent, HandheldRadioFrequencyChange>(OnFrequencyChange);
        SubscribeLocalEvent<HandheldRadioComponent, HandheldRadioStateChange>(OnStateChange);
        SubscribeLocalEvent<HandheldRadioComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<HandheldRadioComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<HandheldRadioComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<HandheldRadioComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerb);

        RefreshCache();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _recentlySent.Clear();
    }

    private void OnGetAlternativeVerb(Entity<HandheldRadioComponent> ent, ref GetVerbsEvent<AlternativeVerb> args){
        AlternativeVerb microphoneSwitch = new()
        {
            Act = () =>
            {
                var ev = new HandheldRadioStateChange(HandheldRadioState.Microphone, !ent.Comp.MicEnabled);
                OnStateChange(ent, ref ev);
                var state = Loc.GetString(ent.Comp.MicEnabled ? "handheld-radio-verb-on-state" : "handheld-radio-verb-off-state");
                var message = Loc.GetString("handheld-radio-verb-mic-switched", ("state", state));
                _popup.PopupEntity(message, ent);

                if (!TryComp(ent, out UserInterfaceComponent? ui_comp) || 
                    !_uiSystem.HasUi(ent, HandheldRadioUiKey.Key))
                    return;
                
                _uiSystem.ServerSendUiMessage((ent, ui_comp), HandheldRadioUiKey.Key, ev);
            },
            Category = VerbCategory.Switch,
            Text = Loc.GetString("handheld-radio-verb-mic"),
            Priority = 2
        };

        AlternativeVerb speakerSwitch = new()
        {
            Act = () =>
            {
                var ev = new HandheldRadioStateChange(HandheldRadioState.Speaker, !ent.Comp.SpeakerEnabled);
                OnStateChange(ent, ref ev);
                var state = Loc.GetString(ent.Comp.SpeakerEnabled ? "handheld-radio-verb-on-state" : "handheld-radio-verb-off-state");
                var message = Loc.GetString("handheld-radio-verb-speaker-switched", ("state", state));
                _popup.PopupEntity(message, ent);

                if (!TryComp(ent, out UserInterfaceComponent? ui_comp) || 
                    !_uiSystem.HasUi(ent, HandheldRadioUiKey.Key))
                    return;
                
                _uiSystem.ServerSendUiMessage((ent, ui_comp), HandheldRadioUiKey.Key, ev);
            },
            Category = VerbCategory.Switch,
            Text = Loc.GetString("handheld-radio-verb-speaker"),
            Priority = 1
        };

        args.Verbs.Add(microphoneSwitch);
        args.Verbs.Add(speakerSwitch);
    }

    private void OnFrequencyChange(Entity<HandheldRadioComponent> ent, ref HandheldRadioFrequencyChange args){
        if (ent.Comp.CurrentFrequency == args.Frequency)
            return;

        ent.Comp.CurrentFrequency = args.Frequency;
        Dirty(ent);
        RefreshCache();
    }

    private void OnStateChange(Entity<HandheldRadioComponent> ent, ref HandheldRadioStateChange args){
        switch(args.State){
            case HandheldRadioState.Microphone:
                if(!ent.Comp.MicEnabled && args.value)
                    EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.MicListeningRange;

                if(ent.Comp.MicEnabled && !args.value)
                    RemCompDeferred<ActiveListenerComponent>(ent);

                ent.Comp.MicEnabled = args.value;
                _appearance.SetData(ent, HandheldRadioVisuals.Microphone, args.value);
                break;
            case HandheldRadioState.Speaker:
                ent.Comp.SpeakerEnabled = args.value;
                _appearance.SetData(ent, HandheldRadioVisuals.Speaker, args.value);
                break;
        }
        Dirty(ent);
        RefreshCache();
    }

    private void OnDropped(Entity<HandheldRadioComponent> ent, ref DroppedEvent args)
    {
        if (!TryComp(ent, out UserInterfaceComponent? ui_comp) || 
            !_uiSystem.HasUi(ent, HandheldRadioUiKey.Key))
            return;

        _uiSystem.CloseUi((ent, ui_comp), HandheldRadioUiKey.Key);
    }

    private void OnExamine(Entity<HandheldRadioComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(HandheldRadioComponent)))
        {
            var freq = ent.Comp.CurrentFrequency.ToString("0.0");
            var mic = ent.Comp.MicEnabled ? "on" : "off";
            var speaker = ent.Comp.SpeakerEnabled ? "on" : "off";

            args.PushMarkup(Loc.GetString("handheld-radio-ui-examine-frequency", ("frequency", freq)));
            args.PushMarkup(Loc.GetString("handheld-radio-ui-examine-mic", ("mic", mic)));
            args.PushMarkup(Loc.GetString("handheld-radio-ui-examine-speaker", ("speaker", speaker)));
        }
    }

    private void OnAttemptListen(Entity<HandheldRadioComponent> ent, ref ListenAttemptEvent args)
    {
        if (!ent.Comp.MicEnabled ||
            HasComp<HandheldRadioComponent>(args.Source) ||
            !TryComp(args.Source, out TransformComponent? source_tf) ||
            !TryComp(ent, out TransformComponent? target_tf) ||
            !_interaction.InRangeUnobstructed((args.Source, source_tf), (ent, target_tf), ent.Comp.MicListeningRange))
                args.Cancel();
    }

    private void OnListen(Entity<HandheldRadioComponent> ent, ref ListenEvent args)
    {
        if (_recentlySent.Add((ent.Comp.CurrentFrequency, args.Source, args.Message)))
            RelayMessage(ent, args.Source, args.Message);
    }

    private void RelayMessage(Entity<HandheldRadioComponent> radio, EntityUid source, string message)
    {
        if (!_frequencyCache.TryGetValue(radio.Comp.CurrentFrequency, out var freq) || 
            freq == null || 
            !TryComp(radio, out TransformComponent? senderTf) ||
            !TryComp(source, out MetaDataComponent? sourceMeta))
            return;
        
        foreach (var targetRadio in freq)
        {
            if (targetRadio == radio ||
                !targetRadio.Comp.SpeakerEnabled ||
                !TryComp(targetRadio, out TransformComponent? targetTf) ||
                !TryComp(targetRadio, out MetaDataComponent? targetMeta))
                continue;

            if (senderTf.MapID != targetTf.MapID && !targetRadio.Comp.RecievesFromAnyMap)
                continue;
            
            var name = Loc.GetString("speech-name-relay", ("speaker", Name(radio, targetMeta)), ("originalName", Name(source, sourceMeta)));
            LanguagePrototype? language = null;
            if (TryComp(source, out LanguageSpeakerComponent? sourceLang))
                language = _language.GetLanguage((source, sourceLang));
            _chat.TrySendInGameICMessage(targetRadio, message, InGameICChatType.Whisper, ChatTransmitRange.GhostRangeLimit, nameOverride: name, checkRadioPrefix: false, languageOverride: language);
        }
    }

    private void RefreshCache()
    {
        _frequencyCache.Clear();
        var query = EntityQueryEnumerator<HandheldRadioComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_frequencyCache.TryGetValue(comp.CurrentFrequency, out var value) || value is null)
                _frequencyCache[comp.CurrentFrequency] = [];
            
            _frequencyCache[comp.CurrentFrequency].Add((uid, comp));
        }
    }
}
