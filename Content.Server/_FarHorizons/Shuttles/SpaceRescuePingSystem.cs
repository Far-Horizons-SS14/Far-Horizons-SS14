using System.Numerics;
using Content.Shared._FarHorizons.Shuttles;
using Content.Shared.Emag.Systems;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Shuttles;

public sealed class SpaceRescuePingSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceRescuePingSenderComponent, GotEmaggedEvent>(OnGotEmagged);
    }

    private void OnGotEmagged(Entity<SpaceRescuePingSenderComponent> ent, ref GotEmaggedEvent args)
    {
        if (!HasComp<BorgTransponderComponent>(ent))
            return;

        _entMan.RemoveComponent<SpaceRescuePingSenderComponent>(ent);
    }

    // I'd love for this to happen on the client, but PVSs ensure that's not happening
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpaceRescuePingReceiverComponent, UserInterfaceComponent, TransformComponent, ActiveUserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var uiComp, out var rcvTransform, out _))
        {
            var isRadar = _ui.IsUiOpen((uid, uiComp), RadarConsoleUiKey.Key);
            var isShuttle = _ui.IsUiOpen((uid, uiComp), ShuttleConsoleUiKey.Key);

            if (!isRadar && !isShuttle)
                continue;

            if (_timing.CurTime < comp.NextRefresh)
                continue;

            comp.NextRefresh = _timing.CurTime + comp.RefreshRate;

            List<(Vector2, Color)> pings = [];

            var pingQuery = EntityQueryEnumerator<SpaceRescuePingSenderComponent, TransformComponent>();
            while (pingQuery.MoveNext(out var pingUid, out var pingComp, out var sndTransform))
            {
                if (pingComp.Hidden ||
                    sndTransform.GridUid != null ||
                    sndTransform.MapID != rcvTransform.MapID)
                    continue;

                var parent = _transform.GetParentUid(pingUid); // Likely a person wearing the suit
                var parentsParent = _transform.GetParentUid(parent); // Container that person is inside of
                if (HasComp<SpaceRescuePingMutedComponent>(parentsParent))
                    continue;

                var netCoords = new NetCoordinates(GetNetEntity(pingUid), _transform.GetWorldPosition(sndTransform));
                pings.Add((netCoords.Position, pingComp.Color));
            }

            if (pings.Count <= 0)
                continue;

            var message = new SpaceRescuePingMessage(pings, comp.RefreshRate);

            if (isRadar)
                _ui.ServerSendUiMessage((uid, uiComp), RadarConsoleUiKey.Key, message);

            if (isShuttle)
                _ui.ServerSendUiMessage((uid, uiComp), ShuttleConsoleUiKey.Key, message);
        }
    }
}