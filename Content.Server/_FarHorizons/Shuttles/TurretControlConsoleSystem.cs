using Content.Shared._FarHorizons.Shuttles;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;

namespace Content.Server._FarHorizons.Shuttles;

public sealed class TurretControlConsoleSystem : EntitySystem
{
    [Dependency] private readonly TurretControlServerSystem _turretControl = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<TurretControlConsoleComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<TurretControlConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TurretControlConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        
        SubscribeLocalEvent<TurretControlConsoleComponent, TurretControlSetTargetMessage>(OnTargetSelect);
        SubscribeLocalEvent<TurretControlConsoleComponent, TurretControlFireCannonsMessage>(OnFireCannons);
    }

    private void OnMapInit(EntityUid uid, TurretControlConsoleComponent comp, ref MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSinkComponent>(uid, out var sink))
            return;
        
        foreach(var source in sink.LinkedSources)
        {
            if (!HasComp<TurretControlServerComponent>(source))
                continue;

            comp.ControlServer = GetNetEntity(source);
            Dirty(uid, comp);
            return;
        }
    }

    private void OnNewLink(EntityUid uid, TurretControlConsoleComponent comp, ref NewLinkEvent args)
    {
        if (!HasComp<TurretControlServerComponent>(args.Source))
            return;

        comp.ControlServer = GetNetEntity(args.Source);
        Dirty(uid, comp);
    }

    private void OnPortDisconnected(EntityUid uid, TurretControlConsoleComponent comp, ref PortDisconnectedEvent args)
    {
        if (args.Port != comp.LinkingPort)
            return;

        comp.ControlServer = null;
        Dirty(uid, comp);
    }

    private void OnTargetSelect(EntityUid uid, TurretControlConsoleComponent comp, ref TurretControlSetTargetMessage args)
    {
        if(!TryComp<TurretControlServerComponent>(GetEntity(comp.ControlServer), out var serverComponent))
            return;

        serverComponent.TargetCoordinates = args.Position;
    }

    private void OnFireCannons(EntityUid uid, TurretControlConsoleComponent comp, ref TurretControlFireCannonsMessage args)
    {
        var server = GetEntity(comp.ControlServer);
        if(server == null)
            return;

        if(!TryComp<TurretControlServerComponent>(server, out var serverComponent))
            return;

        _turretControl.Fire(server.Value, serverComponent);
    }
}