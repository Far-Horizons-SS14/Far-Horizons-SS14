using Content.Server._FarHorizons.Fusion.Components;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Fusion.Systems;

public sealed partial class FusionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private void ProcessFusionDevices()
    {
        var query = EntityQueryEnumerator<FusionDeviceComponent, TransformComponent>();

        var time = _gameTiming.CurTime;
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            var ev = new FusionDeviceUpdateEvent((time-comp.LastProcess).TotalSeconds);
            RaiseLocalEvent(uid, ref ev);
            comp.LastProcess = time;
        }
    }
}