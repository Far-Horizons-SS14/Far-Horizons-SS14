//#region starlight
using System.Linq;
using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
//#endregion

namespace Content.Shared.Speech
{
    public sealed class SpeechSystem : EntitySystem
    {

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!; //#starlight

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<HumanoidCharacterProfileComponent, ApplyOrganMarkingsEvent>(OnAppearanceEmotes);// Far Horizons
        }

        public void SetSpeech(EntityUid uid, bool value, SpeechComponent? component = null)
        {
            if (value && !Resolve(uid, ref component))
                return;

            component = EnsureComp<SpeechComponent>(uid);

            if (component.Enabled == value)
                return;

            component.Enabled = value;

            Dirty(uid, component);
        }

        private void OnSpeakAttempt(SpeakAttemptEvent args)
        {
            if (!TryComp(args.Uid, out SpeechComponent? speech) || !speech.Enabled)
                args.Cancel();
        }

        // Far Horizons start
        private void OnAppearanceEmotes(Entity<HumanoidCharacterProfileComponent> ent, ref ApplyOrganMarkingsEvent args)
        {
            // Fun fact, ApplyOrganMarkingsEvent only has markings in it on the client, on server it's empty. How fun!
            if (!TryComp<SpeechComponent>(ent, out var speech) || ent.Comp.Profile == null)
                return;
            
            var markings = ent.Comp.Profile.Appearance.Markings.Values
                .SelectMany(inner => inner.Values)
                .SelectMany(markingsList => markingsList)
                .Select(marking => _prototypeManager.Index<MarkingPrototype>(marking.MarkingId))
                .ToList();
            AddMarkingEmotes((ent.Owner, speech), markings);
        }
        // Far Horizons end

        #region starlight
        public void AddMarkingEmotes(Entity<SpeechComponent> ent, List<MarkingPrototype> markings)
        {
            HashSet<string> AttachedIds = new();
            foreach (var marking in markings)
            {
                AttachedIds.Add(marking.ID);
            }

            foreach (var marking in markings)
            {
                if (marking.Emotes == null)
                    continue;
                foreach (var emote in marking.Emotes)
                {
                    if (emote.RequiredMarkings == null ||
                        AttachedIds.IsSupersetOf(emote.RequiredMarkings.Select(i => i.Id)))
                    {
                        ent.Comp.AllowedEmotes.Add(emote.EmotePrototype.Id);
                    }

                    if (emote.RequiredMarkingsAny != null)
                    {
                        foreach (var required in emote.RequiredMarkingsAny)
                        {
                            if (AttachedIds.Contains(required.Id))
                            {
                                ent.Comp.AllowedEmotes.Add(emote.EmotePrototype.Id);
                            }
                        }
                    }
                }
            }
        }
        #endregion starlight
    }
}
