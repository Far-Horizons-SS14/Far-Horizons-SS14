using Content.Server._FarHorizons.Shuttles.Components;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Nuke;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Nuke;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Shuttles;

public sealed partial class EscapePodNukeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ShuttleSystem _shuttle = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private EmergencyShuttleSystem _emergencyshuttle = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private AlertLevelSystem _alertLevel = default!;

    private bool _preppods = false;
    private bool _escapealerted = false;
    private EntityUid? _originalstaiton;
    private bool _annoucedlaunch = false;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
        SubscribeLocalEvent<NukeDisarmSuccessEvent>(OnNukeDisarm);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // Don't handle any of this logic if in lobby
        if (_ticker.RunLevel == GameRunLevel.PreRoundLobby)
            return;
        
            var query = EntityQueryEnumerator<NukeComponent>();
        while (query.MoveNext(out var uid, out var nuke))
        {
            if (nuke.Status != NukeStatus.ARMED)
                continue;

            if (nuke.RemainingTime <= nuke.DisarmDoAfterLength && !_escapealerted)
            {
                _chatSystem.DispatchGlobalAnnouncement(
                    Loc.GetString("fh-nuke-component-announcement-evac", ("time", (int)nuke.DisarmDoAfterLength)),
                    playSound: false,
                    colorOverride: Color.Red);

                _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), recordReplay: true);

                _escapealerted = true;
            }

            if (nuke.RemainingTime <= 8) //Under no cercumstances should the nuke be capable of going off before FTL, physics are weird and they still get destroyed
                LaunchPods();
        }
    }

    private void LaunchPods()
    {
        //mostly copied from EmergencyShuttleSystem, but with only the pod launch parts and modified to function independatly

        var podQuery = AllEntityQuery<EscapePodComponent>();

        var podLaunchQuery = EntityQueryEnumerator<EscapePodComponent, ShuttleComponent>();

        // Stagger launches coz funny
        if (!_preppods)
        {
            _preppods = true;
            while (podQuery.MoveNext(out var uid, out var pod))
            {
                pod.LaunchTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(0.05f, 0.75f));
                AddComp<AlternativeEscapeComponent>(uid);
            }
        }

        int timeDelay = 0; //used to stagger arrival times
        while (podLaunchQuery.MoveNext(out var uid, out var pod, out var shuttle))
        {
            var stationUid = _station.GetOwningStation(uid);

            if (!TryComp<StationCentcommComponent>(stationUid, out var centcomm) ||
                Deleted(centcomm.Entity) ||
                pod.LaunchTime == null ||
                pod.LaunchTime > _timing.CurTime)
            {
                continue;
            }

            _shuttle.FTLToDock(uid, shuttle, centcomm.Entity.Value, hyperspaceTime: _emergencyshuttle.TransitTime + timeDelay++);
            RemCompDeferred<EscapePodComponent>(uid);
            _originalstaiton = stationUid;
        }
    }

    private void OnNukeExploded(NukeExplodedEvent ev)
    {
        if (_annoucedlaunch) 
            return;

        _annoucedlaunch = true;
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("fh-escape-pods-left", ("transitTime", $"{_emergencyshuttle.TransitTime:0}")));
        var stationUid = _station.GetOwningStation(_originalstaiton);
        if (stationUid != null)
            _alertLevel.SetLevel(stationUid.Value, "red", false, false, true); //secretly change alert back to red so the lights come back on
    }

    private void OnNukeDisarm(NukeDisarmSuccessEvent ev)
    {
        _preppods = false;
        _escapealerted = false;
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        _preppods = false;
        _escapealerted = false;
        _annoucedlaunch = false;
        _originalstaiton = null;
    }
}