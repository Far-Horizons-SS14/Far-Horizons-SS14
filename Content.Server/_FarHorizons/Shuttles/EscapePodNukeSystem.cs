using Content.Server.AlertLevel;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Nuke;
using Content.Shared.Shuttles.Components;
using Microsoft.Extensions.Configuration;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Nuke;

public sealed class EscapePodNukeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyshuttle = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;

    private bool _preppods = false;
    private EntityUid? _originalstaiton;
    private bool _annoucedlaunch = false;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // Don't handle any of this logic if in lobby
        if (_ticker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            var query = EntityQueryEnumerator<NukeComponent>();
            while (query.MoveNext(out var uid, out var nuke))
            {
                if (nuke.Status == NukeStatus.ARMED)
                {
                    if (nuke.RemainingTime <= nuke.DisarmDoAfterLength && !nuke.EscapeAlerted)
                    {
                        _chatSystem.DispatchGlobalAnnouncement(
                            Loc.GetString("fh-nuke-component-announcement-evac", ("time", (int)nuke.DisarmDoAfterLength)),
                            playSound: false,
                            colorOverride: Color.Red);

                        _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), recordReplay: true);

                        nuke.EscapeAlerted = true;
                    }

                    if (nuke.RemainingTime <= 8) //Under no cercumstances should the nuke be capable of going off before FTL, physics are weird and they still get destroyed
                        LaunchPods();
                }
            }
        }
    }

    private void LaunchPods()
    {
        //mostly copied from EmergencyShuttleSystem, but with only the pod launch parts and modified to function independatly, also only launches for the current station

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
        if (!_annoucedlaunch)
        {
            _annoucedlaunch = true;
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("fh-escape-pods-left", ("transitTime", $"{_emergencyshuttle.TransitTime:0}")));
            var stationUid = _station.GetOwningStation(_originalstaiton);
            if (stationUid != null)
                _alertLevel.SetLevel(stationUid.Value, "red", false, false, true); //secretly change alert back to red so the lights come back on
        }
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        _preppods = false;
        _annoucedlaunch = false;
    }
}