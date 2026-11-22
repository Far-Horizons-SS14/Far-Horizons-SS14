using Content.Server._Starlight.Language;
using Content.Server._Starlight.Speech.EntitySystems;
using Content.Server.Popups;
using Content.Server.Speech.EntitySystems;
using Content.Shared._FarHorizons.Medical.Trauma;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech;

namespace Content.Server._FarHorizons.Medical.Trauma;

public sealed partial class ServerTraumaSystem : TraumaSystem
{
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private LanguageSystem _language = default!;

    // Below basically a copy of Mutable system, keeping is separate for limb statuses helps with keeping track of it, as mutable is added and removed by different things 

    [SubscribeLocalEvent(before: [typeof(VocalSystem), typeof(MumbleAccentSystem)])]
    private void OnEmote(Entity<SurgeryLimbStatusMuteComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        //still leaves the text so it looks like they are pantomiming a laugh
        if (args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            args.Handled = true;
    }

    [SubscribeLocalEvent(before: [typeof(VocalSystem)])]
    private void OnScreamAction(Entity<SurgeryLimbStatusMuteComponent> ent, ref ScreamActionEvent args)
    {
        if (args.Handled)
            return;

        _popup.PopupEntity(Loc.GetString("speech-muted"), ent, ent);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnSpeakAttempt(Entity<SurgeryLimbStatusMuteComponent> ent, ref SpeakAttemptEvent args)
    {
        var language = _language.GetLanguage(ent.Owner);
        if (!language.SpeechOverride.RequireSpeech)
            return;

        _popup.PopupEntity(Loc.GetString("speech-muted"), ent.Owner, ent.Owner);

        args.Cancel();
    }
}