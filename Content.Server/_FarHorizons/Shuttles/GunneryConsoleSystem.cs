using System.Linq;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Shuttles.Systems;
using Content.Shared._FarHorizons.Shuttles;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Physics;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Shuttles;

public sealed class GunneryConsoleSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signal = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = null!;

    private const CollisionGroup BulletCollisionMask = CollisionGroup.Impassable | CollisionGroup.BulletImpassable;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunneryConsoleComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<GunneryConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<GunneryConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<GunneryConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<GunneryConsoleComponent, GunneryConsoleFireActionMessage>(OnFireAction);
        SubscribeLocalEvent<GunneryConsoleComponent, GunneryConsoleTargetActionMessage>(OnTargetAction);
        SubscribeLocalEvent<GunneryConsoleComponent, GunneryConsoleSelectActionMessage>(OnSelectAction);
    }

    private void OnMapInit(EntityUid uid, GunneryConsoleComponent comp, ref MapInitEvent args)
    {
        _signal.EnsureSourcePorts(uid, comp.TurretConnectionPort);

        CollectTurrets(uid, comp);
    }

    private void CollectTurrets(EntityUid uid, GunneryConsoleComponent comp)
    {
        if (!EntityManager.TryGetComponent<DeviceLinkSourceComponent>(uid, out var source))
            return;

        comp.ConnectedTurrets.Clear();

        foreach (var (turretUid, _) in source.LinkedPorts)
        {
            if (!EntityManager.HasComponent<GunComponent>(turretUid))
                continue;

            comp.ConnectedTurrets.Add(turretUid);
        }
    }

    private void OnNewLink(EntityUid uid, GunneryConsoleComponent comp, ref NewLinkEvent args)
    {
        if (args.SourcePort != comp.TurretConnectionPort)
            return;

        if (!EntityManager.HasComponent<GunComponent>(args.Sink))
            return;

        if (comp.ConnectedTurrets.Contains(args.Sink))
            return;

        comp.ConnectedTurrets.Add(args.Sink);
        UpdateUI(uid, comp);
    }

    private void OnPortDisconnected(EntityUid uid, GunneryConsoleComponent comp, ref PortDisconnectedEvent args)
    {
        if (args.Port != comp.TurretConnectionPort)
            return;

        comp.ConnectedTurrets.Remove(args.Sink);
        UpdateUI(uid, comp);
    }

    private void OnUIOpened(EntityUid uid, GunneryConsoleComponent comp, ref BoundUIOpenedEvent args) => UpdateUI(uid, comp);

    private void UpdateUI(EntityUid uid, GunneryConsoleComponent comp)
    {
        if (!_uiSystem.IsUiOpen(uid, GunneryConsoleUiKey.Key))
            return;

        GunneryConsoleBuiState state = new();
        comp.ConnectedTurrets.ForEach(turret =>
            state.TurretEntities.Add(new GunneryConsoleTurretEntry(
                GetNetEntity(turret),
                _gunSystem.GetAmmoCount(turret),
                _gunSystem.GetAmmoCapacity(turret)
            ))
        );
        UpdateTurretMetaData(uid, comp);
        state.Selected = comp.SelectedTurrets;
        state.State = GetNavState(uid);
        _uiSystem.SetUiState(uid, GunneryConsoleUiKey.Key, state);
    }

    private void UpdateTurretMetaData(EntityUid uid, GunneryConsoleComponent comp)
    {
        comp.TurretMetaData.Clear();

        foreach (var turret in comp.ConnectedTurrets)
        {
            var metaData = new GunneryConsoleTurretMetaData
            {
                EntityName = Identity.Name(turret, EntityManager),
                Coordinates = EntityManager.GetNetCoordinates(Transform(turret).Coordinates)
            };
            comp.TurretMetaData[GetNetEntity(turret)] = metaData;
        }

        Dirty(uid, comp);
    }

    private float _accumulator = 0f;
    private readonly float _threshold = 0.5f;

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator > _threshold)
        {
            AccUpdate();
            _accumulator -= _threshold;
        }

        return;

        void AccUpdate()
        {
            var query = EntityQueryEnumerator<GunneryConsoleComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                UpdateUI(uid, component);
            }
        }
    }

    private NavInterfaceState GetNavState(EntityUid uid)
    {
        var xform = Transform(uid);
        var onGrid = xform.ParentUid == xform.GridUid;
        EntityCoordinates? coordinates = onGrid ? xform.Coordinates : null;
        Angle? angle = onGrid ? xform.LocalRotation : null;

        return coordinates != null && angle != null
            ? _console.GetNavState(uid, _console.GetAllDocks(), coordinates.Value, angle.Value)
            : _console.GetNavState(uid, _console.GetAllDocks());
    }

    private void OnFireAction(EntityUid uid, GunneryConsoleComponent comp, ref GunneryConsoleFireActionMessage args)
    {
        List<EntityUid> turretUids = [];
        args.TurretEntities.ForEach(netEnt => turretUids.Add(EntityManager.GetEntity(netEnt)));
        turretUids = [.. turretUids.Intersect(comp.ConnectedTurrets)];

        var targetCoords = EntityManager.GetCoordinates(args.Position);
        var targetWorldCoords = _transformSystem.ToWorldPosition(targetCoords);

        foreach (var turret in turretUids)
        {
            var xform = Transform(turret);
            if (!EntityManager.TryGetComponent<GunComponent>(turret, out var gun) || xform == null)
                continue;

            if (!_gunSystem.CanShoot(gun))
                continue;

            var globalPos = _transformSystem.GetWorldPosition(turret);
            var targetDir = targetWorldCoords - globalPos;
            targetDir.Normalize();

            // Rays have no width, bullets do, this is to compensate.
            // The bullet is only 0.1 wide, but 0.2 gives buffer for jank collisions and ship movement
            var posOffset = (targetDir with { X = -targetDir.X }) * 0.2f;

            var ray1 = new CollisionRay(globalPos + posOffset, targetDir, (int)BulletCollisionMask);
            var ray1CastResults = _physics.IntersectRay(xform.MapID, ray1, comp.CheckDistance, turret, false);

            if (ray1CastResults.Select(r => r.HitEntity).Any(u => Transform(u).ParentUid == xform.ParentUid))
                continue;

            var ray2 = new CollisionRay(globalPos - posOffset, targetDir, (int)BulletCollisionMask);
            var ray2CastResults = _physics.IntersectRay(xform.MapID, ray2, comp.CheckDistance, turret, false);

            if (ray2CastResults.Select(r => r.HitEntity).Any(u => Transform(u).ParentUid == xform.ParentUid))
                continue;

            // Random delay between 0 and 300ms to make the firing feel less artificial
            Timer.Spawn(_random.Next(0, 300), () =>
                _gunSystem.AttemptShoot(turret, (turret, gun), targetCoords)
            );
        }
    }

    private void OnTargetAction(EntityUid uid, GunneryConsoleComponent comp, ref GunneryConsoleTargetActionMessage args) 
    {
        comp.TargetPosition = args.Position;
        DirtyField(uid, comp, nameof(GunneryConsoleComponent.TargetPosition));
    }

    private void OnSelectAction(EntityUid uid, GunneryConsoleComponent comp, ref GunneryConsoleSelectActionMessage args)
    {
        foreach(var (ent, add) in args.TurretEntities)
        {
            if (add)
            {
                if(!comp.SelectedTurrets.Contains(ent))
                    comp.SelectedTurrets.Add(ent);
            }
            else
                comp.SelectedTurrets.Remove(ent);
        }
        UpdateUI(uid, comp);
    }
}