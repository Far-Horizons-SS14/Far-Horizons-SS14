using System.Linq;
using Content.Shared._FarHorizons.Shuttles;
using Robust.Client.GameObjects;

namespace Content.Client._FarHorizons.Shuttles;

public sealed class GunneryConsoleSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunneryConsoleComponent, AfterAutoHandleStateEvent>(OnStateUpdate);

        SubscribeLocalEvent<GunneryConsoleComponent, ComponentShutdown>(OnShutdown);
        // SubscribeLocalEvent<GunneryConsoleComponent, GunneryConsoleTargetActionMessage>(OnTargetAction);
    }

    private void OnStateUpdate(EntityUid uid, GunneryConsoleComponent comp, ref AfterAutoHandleStateEvent args) => RotateTurrets(comp);

    private void OnShutdown(EntityUid uid, GunneryConsoleComponent comp, ref ComponentShutdown args)
    {
        // Panic set everything to home
        foreach (var (turret, _) in comp.MovingTurrets)
        {
            _spriteSystem.SetRotation(turret, 0);
            comp.MovingTurrets[turret] = null;
        }
    }

    private void RotateTurrets(GunneryConsoleComponent comp)
    {
        var turretUids = comp.SelectedTurrets.Select(EntityManager.GetEntity);

        foreach (var turret in turretUids)
        {
            if(!TryComp<SpriteComponent>(turret, out var sprite))
                continue;

            comp.MovingTurrets[turret] = comp.TargetPosition;
        }

        // Turrets that are no longer selected and should be moved home
        foreach (var turret in comp.MovingTurrets.Keys.Except(turretUids))
            comp.MovingTurrets[turret] = null;
    }

    private void OnTargetAction(EntityUid uid, GunneryConsoleComponent comp, ref GunneryConsoleTargetActionMessage args) 
    {
        comp.TargetPosition = args.Position;
        RotateTurrets(comp);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<GunneryConsoleComponent>();
        while (query.MoveNext(out var comp))
        {
            if(comp.MovingTurrets.Count <= 0)
                return;

            List<EntityUid> removable = [];
            foreach (var (turret, target) in comp.MovingTurrets)
            {
                if(!TryComp<SpriteComponent>(turret, out var sprite))
                    continue;

                var (turretPos, turretRot) = _transformSystem.GetWorldPositionRotation(turret);

                var targetRot = target == null ? 0 : Angle.FromWorldVec(target.Value - turretPos) - turretRot;
                var rotationDiff = Angle.ShortestDistance(sprite.Rotation, targetRot).Theta;
                var maxRotate = MathHelper.DegreesToRadians(comp.MoveSpeed) * frameTime;

                if (Math.Abs(rotationDiff) > maxRotate)
                {
                    var goalTheta = sprite.Rotation + (Math.Sign(rotationDiff) * maxRotate);
                    _spriteSystem.SetRotation(turret, goalTheta);

                    continue;
                }

                _spriteSystem.SetRotation(turret, targetRot);

                if(target == null)
                    removable.Add(turret); // We don't want to lose track of this turret until it's safely back at its home position
            }

            foreach(var remove in removable)
            {
                comp.MovingTurrets.Remove(remove);
            }
        }
    }
}