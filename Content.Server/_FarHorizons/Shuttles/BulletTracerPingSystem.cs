using System.Numerics;
using Content.Shared._FarHorizons.Shuttles;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Shuttles;

/// <summary>
/// Shameless copy of the <see cref="SpaceRescuePingSystem"/>
/// </summary>
public sealed class BulletTracerPingSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BulletTracerReceiverComponent, UserInterfaceComponent, TransformComponent, ActiveUserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var uiComp, out var rcvTransform, out _))
        {
            if (!_ui.IsUiOpen((uid, uiComp), GunneryConsoleUiKey.Key))
                continue;

            if (_timing.CurTime < comp.NextRefresh)
                continue;

            comp.NextRefresh = _timing.CurTime + comp.RefreshRate;

            List<(Vector2, Color)> pings = [];

            var pingQuery = EntityQueryEnumerator<BulletTracerSenderComponent, TransformComponent>();
            while (pingQuery.MoveNext(out var pingUid, out var pingComp, out var sndTransform))
            {
                if (sndTransform.GridUid != null ||
                    sndTransform.MapID != rcvTransform.MapID)
                    continue;

                var netCoords = new NetCoordinates(GetNetEntity(pingUid), _transform.GetWorldPosition(sndTransform));
                pings.Add((netCoords.Position, pingComp.Color));
            }

            if (pings.Count <= 0)
                continue;

            var message = new BulletTracerPingMessage(pings);

            _ui.ServerSendUiMessage((uid, uiComp), GunneryConsoleUiKey.Key, message);
        }
    }
}