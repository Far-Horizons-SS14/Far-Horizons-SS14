using Content.Shared.Communications;
using Robust.Server.GameObjects;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Radio.Components;
using Content.Server.Construction;
using Content.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Station.Components;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Server.Starlight.TTS;
using Robust.Shared.Utility; 
using Content.Server.AlertLevel;
using Content.Server.RoundEnd;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Content.Server.Shuttles.Systems;
using Robust.Shared.Log;

namespace Content.Server.Communications
{
    public sealed class DepartmentalAnnouncementSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
        private const float UIUpdateInterval = 5.0f;
        public override void Initialize()
        {
            SubscribeLocalEvent<DepartmentalAnnouncementComponent, MapInitEvent>(OnCommunicationsConsoleMapInit);
            SubscribeLocalEvent<DepartmentalAnnouncementComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
            SubscribeLocalEvent<DepartmentalAnnouncementComponent, ItemSlotEjectAttemptEvent>(OnEjectAttempt);
            SubscribeLocalEvent<DepartmentalAnnouncementComponent, ConstructionChangeEntityEvent>(OnConstructionChangeEntityEvent);
            SubscribeLocalEvent<DepartmentalAnnouncementComponent, CommunicationsConsoleSelectAnnouncementChannel>(OnSelectAnnouncementChannel);
        }

        public void OnCommunicationsConsoleMapInit(EntityUid uid, DepartmentalAnnouncementComponent comp, MapInitEvent args)
        {
            UpdateCommsConsoleInterface(uid, comp);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<DepartmentalAnnouncementComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                comp.UIUpdateAccumulator += frameTime;

                if (comp.UIUpdateAccumulator < UIUpdateInterval)
                    continue;

                comp.UIUpdateAccumulator -= UIUpdateInterval;

                if (_uiSystem.IsUiOpen(uid, CommunicationsConsoleUiKey.Key))
                    UpdateCommsConsoleInterface(uid, comp);
            }

            base.Update(frameTime);
        }

        public void UpdateCommsConsoleInterface(EntityUid uid, DepartmentalAnnouncementComponent comp)
        {

            var stationUid = _stationSystem.GetOwningStation(uid);
            List<string>? levels = null;
            string currentLevel = default!;
            float currentDelay = 0;

            if (stationUid != null)
            {
                if (TryComp(stationUid.Value, out AlertLevelComponent? alertComp) &&
                    alertComp.AlertLevels != null)
                {
                    if (alertComp.IsSelectable)
                    {
                        levels = new();
                        foreach (var (id, detail) in alertComp.AlertLevels.Levels)
                        {
                            if (detail.Selectable)
                            {
                                levels.Add(id);
                            }
                        }
                    }

                    currentLevel = alertComp.CurrentLevel;
                    currentDelay = _alertLevelSystem.GetAlertLevelDelay(stationUid.Value, alertComp);
                }
            }

            bool hasCommon = false;
            if (TryComp<ItemSlotsComponent>(uid, out var itemSlots))
            {
                foreach (var id in itemSlots.Slots.Values)
                {
                    var keyUid = id.ContainerSlot!.ContainedEntity;
                    if (TryComp<EncryptionKeyComponent>(keyUid, out var keyComp))
                    {
                        comp.Channels = new();
                        foreach (var channel in keyComp.Channels)
                        {
                            comp.Channels.Add(channel);
                            if (channel == "Common")
                                hasCommon = true;
                        }
                        if (!hasCommon)
                            comp.Channels.Add("Common");

                        if (comp.CurrentChannel == "No Channels Available")
                        {
                            comp.CurrentChannel = "Common";
                        }
                    }
                }
            }

            var canAnnounce = false;
            var canCallorRecall = false;

            if (TryComp<CommunicationsConsoleComponent>(uid, out var commsComp))
            {
                canAnnounce = CanAnnounce(commsComp);
                canCallorRecall = CanCallOrRecall(commsComp);
            }

            _uiSystem.SetUiState(uid, CommunicationsConsoleUiKey.Key, new CommunicationsConsoleInterfaceState(
                canAnnounce,
                canCallorRecall,
                levels,
                currentLevel,
                currentDelay,
                comp.Channels,
                comp.CurrentChannel,
                _roundEndSystem.ExpectedCountdownEnd
            ));
        }

        private static bool CanAnnounce(CommunicationsConsoleComponent comp)
        {
            return comp.AnnouncementCooldownRemaining <= 0f;
        }

        private bool CanCallOrRecall(CommunicationsConsoleComponent comp)
        {
            // Defer to what the round end system thinks we should be able to do.
            if (_emergency.EmergencyShuttleArrived || !_roundEndSystem.CanCallOrRecall())
                return false;

            // Ensure that we can communicate with the shuttle (either call or recall)
            if (!comp.CanShuttle)
                return false;

            // Calling shuttle checks
            if (_roundEndSystem.ExpectedCountdownEnd is null)
                return true;

            // Recalling shuttle checks
            var recallThreshold = _cfg.GetCVar(CCVars.EmergencyRecallTurningPoint);

            // shouldn't really be happening if we got here
            if (_roundEndSystem.ShuttleTimeLeft is not { } left
                || _roundEndSystem.ExpectedShuttleLength is not { } expected)
                return false;

            return !(left.TotalSeconds / expected.TotalSeconds < recallThreshold);
        }

        private void OnSelectAnnouncementChannel(EntityUid uid, DepartmentalAnnouncementComponent comp, CommunicationsConsoleSelectAnnouncementChannel message)
        {
            comp.CurrentChannel = message.Channel;
            UpdateCommsConsoleInterface(uid, comp);
        }

        private void OnInsertAttempt(EntityUid uid, DepartmentalAnnouncementComponent comp, ref ItemSlotInsertAttemptEvent args)
        {
            Timer.Spawn(0, () => UpdateCommsConsoleInterface(uid, Comp<DepartmentalAnnouncementComponent>(uid)));
        }

        private void OnEjectAttempt(EntityUid uid, DepartmentalAnnouncementComponent comp, ref ItemSlotEjectAttemptEvent args)
        {
            comp.CurrentChannel = "No Channels Available";
            comp.Channels = new List<string> { "No Channels Available" };
            Timer.Spawn(0, () => UpdateCommsConsoleInterface(uid, Comp<DepartmentalAnnouncementComponent>(uid)));
        }

        private void OnConstructionChangeEntityEvent(EntityUid uid, DepartmentalAnnouncementComponent comp, ref ConstructionChangeEntityEvent args)
        {
            var newUid = args.New;
            if (newUid == null)
                return;

            if (TryComp<ContainerFillComponent>(newUid, out var ContainerComp))
            {
                foreach (var id in ContainerComp.Containers)
                {
                    ContainerComp.Containers.Remove(id.Key);
                }
            }
        }

        public void DispatchFilteredCommunicationsConsoleAnnouncement(
            string channel,
            EntityUid source,
            string message,
            string? sender = null,
            bool playSound = true,
            SoundSpecifier? announcementSound = null,
            Color? colorOverride = null,
            bool Global = false)
        {
            sender ??= Loc.GetString("chat-manager-sender-announcement");
            var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", FormattedMessage.EscapeText(message)));

            var inStation = Filter.Broadcast();
            if (!Global)
            {
                var station = _stationSystem.GetOwningStation(source);

                if (station == null)
                {
                    // you can't make a communications console announcement without a station
                    return;
                }
                if (!EntityManager.TryGetComponent<StationDataComponent>(station, out var stationDataComp)) return;
                inStation = _stationSystem.GetInStation(stationDataComp);
            }
            var filter = ChannelFilter(channel, inStation);

            // Custom behavior: For example, change the chat channel or message formatting here if needed
            _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, source, false, true, colorOverride);

            if (playSound)
            {
                var commsConsoleSound = announcementSound ?? new SoundPathSpecifier("/Audio/_Starlight/Announcements/announce2.ogg");
                var resolvedSound = _audio.ResolveSound(commsConsoleSound);
                _audio.PlayGlobal(resolvedSound, filter, true, AudioParams.Default.WithVolume(-2f));
            }

            RaiseLocalEvent(new AnnouncementSpokeEvent
            {
                AnnouncementSound = announcementSound,
                Message = message,
                Source = filter
            });
        }

        private Filter ChannelFilter(string channel, Filter filter)
        {
            filter.RemoveWhere(session =>
            {
                var uid = session.AttachedEntity;
                if (uid == null)
                    return true;

                if (TryComp<WearingHeadsetComponent>(uid.Value, out var headsetEntity) && TryComp<ActiveRadioComponent>(headsetEntity.Headset, out var keyComp))
                {
                    foreach (var id in keyComp.Channels)
                    {
                        if (channel == id)
                            return false;
                    }
                }
                if (TryComp<ActiveRadioComponent>(uid.Value, out var ActiveRadio))
                {
                    if (ActiveRadio.ReceiveAllChannels)
                    {
                        return false;
                    }
                    foreach (var id in ActiveRadio.Channels)
                    {
                        if (channel == id)
                            return false;
                    }
                }
                return true;
            });

            return filter;
        }
    }
}