using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Shuttles.Components;
using Microsoft.Extensions.Configuration;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Nuke;

public sealed class EscapePodNukeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyshuttle = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeExplodedEvent>(OnNuke);
    }

//This is seperate so we can add other things that launch the pods early
    private void OnNuke(NukeExplodedEvent args) => LaunchPods(args.OwningStation);

    private void LaunchPods(EntityUid? station)
    {
        //mostly copied from EmergencyShuttleSystem, but with only the pod launch parts and modified to function independatly

        var podLaunchQuery = EntityQueryEnumerator<EscapePodComponent, ShuttleComponent>();

        int timeDelay = 0; //used to stagger arrival times
        while (podLaunchQuery.MoveNext(out var uid, out var pod, out var shuttle))
        {
            var stationUid = _station.GetOwningStation(uid);

            if (!TryComp<StationCentcommComponent>(stationUid, out var centcomm) ||
                Deleted(centcomm.Entity))
            {
                continue;
            }

            // Don't dock them. If you do end up doing this then stagger launch.
            _shuttle.FTLToDock(uid, shuttle, centcomm.Entity.Value, hyperspaceTime: _emergencyshuttle.TransitTime + timeDelay++);
            RemCompDeferred<EscapePodComponent>(uid);
        }
    }

    
} 