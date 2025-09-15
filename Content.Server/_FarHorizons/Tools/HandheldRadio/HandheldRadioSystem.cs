using Robust.Server.GameObjects;
using Content.Shared.FarHorizons.Tools.HandheldRadio.Components;
using Content.Shared.FarHorizons.Tools.HandheldRadio;
using Content.Shared.Interaction.Events;
using Content.Shared.Examine;
using Content.Shared.Speech;
using Content.Server.Interaction;
using Content.Shared.Speech.Components;
using Content.Shared._Starlight.Language;
using Content.Shared.Chat;
using Content.Server._Starlight.Language;
using Content.Server.Chat.Systems;

namespace Content.Server.FarHorizons.Tools.HandheldRadio.Systems;

public sealed class HandheldRadioSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private Dictionary<float, HashSet<HandheldRadioComponent>> frequencyCache = new();

    private HashSet<(float, EntityUid, string)> _recentlySent = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandheldRadioComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HandheldRadioComponent, HandheldRadioFrequencyChange>(OnFrequencyChange);
        SubscribeLocalEvent<HandheldRadioComponent, HandheldRadioStateChange>(OnStateChange);
        SubscribeLocalEvent<HandheldRadioComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<HandheldRadioComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<HandheldRadioComponent, ListenEvent>(OnListen);

        foreach (HandheldRadioComponent radio in EntityManager.EntityQuery<HandheldRadioComponent>()){
            AddFrequencyCache(radio);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _recentlySent.Clear();
    }

    private void OnFrequencyChange(Entity<HandheldRadioComponent> uid, ref HandheldRadioFrequencyChange args){
        if (uid.Comp.CurrentFrequency == args.Frequency)
            return;

        RemoveFrequencyCache(uid);

        uid.Comp.CurrentFrequency = args.Frequency;
        Dirty(uid, uid.Comp);

        if (uid.Comp.SpeakerEnabled)
            AddFrequencyCache(uid);
    }

    private void OnStateChange(Entity<HandheldRadioComponent> uid, ref HandheldRadioStateChange args){
        switch(args.State){
            case HanheldRadioState.Microphone:
                if(!uid.Comp.MicEnabled && args.value)
                    EnsureComp<ActiveListenerComponent>(uid).Range = uid.Comp.MicListeningRange;

                if(uid.Comp.MicEnabled && !args.value)
                    RemCompDeferred<ActiveListenerComponent>(uid);

                uid.Comp.MicEnabled = args.value;
                break;
            case HanheldRadioState.Speaker:
                if (!uid.Comp.SpeakerEnabled && args.value)
                    AddFrequencyCache(uid.Comp, true);
                
                if (uid.Comp.SpeakerEnabled && !args.value)
                    RemoveFrequencyCache(uid.Comp);
                
                uid.Comp.SpeakerEnabled = args.value;
                break;
        }
        Dirty(uid, uid.Comp);
    }

    private void OnDropped(Entity<HandheldRadioComponent> uid, ref DroppedEvent args)
    {
        if (!TryComp(uid, out UserInterfaceComponent? ui_comp) || 
            !_uiSystem.HasUi(uid, HandheldRadioUiKey.Key))
            return;

        _uiSystem.CloseUi((uid, ui_comp), HandheldRadioUiKey.Key);
    }

    private void OnExamine(Entity<HandheldRadioComponent> uid, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(HandheldRadioComponent)))
        {
            string freq = uid.Comp.CurrentFrequency.ToString("0.0");
            string mic = uid.Comp.MicEnabled ? "on" : "off";
            string speaker = uid.Comp.SpeakerEnabled ? "on" : "off";

            args.PushMarkup(Loc.GetString("handheld-radio-ui-examine-frequency", ("frequency", freq)));
            args.PushMarkup(Loc.GetString("handheld-radio-ui-examine-mic", ("mic", mic)));
            args.PushMarkup(Loc.GetString("handheld-radio-ui-examine-speaker", ("speaker", speaker)));
        }
    }

    private void OnAttemptListen(Entity<HandheldRadioComponent> uid, ref ListenAttemptEvent args)
    {
        if (!uid.Comp.MicEnabled ||
            HasComp<HandheldRadioComponent>(args.Source) ||
            !TryComp(args.Source, out TransformComponent? source_tf) ||
            !TryComp(uid, out TransformComponent? target_tf) ||
            !_interaction.InRangeUnobstructed((args.Source, source_tf), (uid, target_tf), uid.Comp.MicListeningRange))
                args.Cancel();
    }

    private void OnListen(Entity<HandheldRadioComponent> uid, ref ListenEvent args)
    {
        if (_recentlySent.Add((uid.Comp.CurrentFrequency, args.Source, args.Message)))
            RelayMessage(uid.Comp.CurrentFrequency, args.Source, uid, args.Message);
    }

    private void RelayMessage(float frequency, EntityUid source, Entity<HandheldRadioComponent> sender, string message){
        if (!frequencyCache.ContainsKey(frequency) || frequencyCache[frequency] is null)
            return;

        foreach (HandheldRadioComponent radio in frequencyCache[frequency]){
            if (radio.Owner == sender.Owner)
                continue;

            if (Transform(sender).MapID != Transform(radio.Owner).MapID && !radio.RecievesFromAnyMap)
                continue;

            var name = Loc.GetString("speech-name-relay", ("speaker", Name(radio.Owner)), ("originalName", Name(source)));
            LanguagePrototype language = _language.GetLanguage(source);
            _chat.TrySendInGameICMessage(radio.Owner, message, InGameICChatType.Whisper, ChatTransmitRange.GhostRangeLimit, nameOverride: name, checkRadioPrefix: false, languageOverride: language);
        }
    }

    private void AddFrequencyCache(HandheldRadioComponent radio, bool force = false){
        if (!radio.SpeakerEnabled && !force)
            return;

        if (!frequencyCache.ContainsKey(radio.CurrentFrequency) || frequencyCache[radio.CurrentFrequency] is null)
            frequencyCache[radio.CurrentFrequency] = new HashSet<HandheldRadioComponent>();
        
        if (!frequencyCache[radio.CurrentFrequency].Contains(radio))
            frequencyCache[radio.CurrentFrequency].Add(radio);
    }

    private void RemoveFrequencyCache(HandheldRadioComponent radio){
        if (!frequencyCache.ContainsKey(radio.CurrentFrequency) || frequencyCache[radio.CurrentFrequency] is null ||
            !frequencyCache[radio.CurrentFrequency].Contains(radio))
            return;

        frequencyCache[radio.CurrentFrequency].Remove(radio);

        if (frequencyCache[radio.CurrentFrequency].Count == 0)
            frequencyCache.Remove(radio.CurrentFrequency);
    }

}
