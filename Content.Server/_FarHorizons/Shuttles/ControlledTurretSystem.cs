using Content.Shared._FarHorizons.Shuttles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;

namespace Content.Server._FarHorizons.Shuttles;

public sealed class ControlledTurretSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ControlledTurretComponent, AnchorStateChangedEvent>(OnAnchor);
        SubscribeLocalEvent<ControlledTurretComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<ControlledTurretComponent, GunShotEvent>(OnShot);
    }

    private void OnAnchor(EntityUid uid, ControlledTurretComponent comp, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            if(!TryComp(uid, out TransformComponent? xform))
                return;
            
            comp.LocalRot = xform.LocalRotation;
            return;
        }

        comp.LocalRot = Angle.Zero;
        comp.TargetRotation = null;
        comp.TargetCoordinates = null;
        InvalidateSelf(uid, comp);
    }

    private void OnCompRemove(EntityUid uid, ControlledTurretComponent comp, ref ComponentRemove args) => InvalidateSelf(uid, comp);

    /// <summary>
    /// Removes a turret from its controlling server
    /// </summary>
    private static void InvalidateSelf(EntityUid uid, ControlledTurretComponent comp)
    {
        if(comp.ControlServer == null)
            return;

        comp.ControlServer.Value.Comp.InvalidTurrets.Add((uid, comp));
        comp.ControlServer = null;
    }

    private void OnShot(EntityUid uid, ControlledTurretComponent comp, ref GunShotEvent args) => comp.State = ControlledTurretFiringState.Ready; 

    public override void Update(float frameTime)
    {
        // Yes, this has to be run every update or else things get weird
        var query = EntityQueryEnumerator<ControlledTurretComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            // Don't try to move a gun that's shooting
            if(comp.State == ControlledTurretFiringState.Firing)
                continue;

            if(!xform.Anchored)
                continue;

            if(comp.TargetRotation == null)
            {
                var goalRot = ToPi(comp.LocalRot.Theta);
                
                if(comp.TargetCoordinates != null)
                {
                    var globalPos = _transformSystem.GetWorldPosition(uid);
                    var (_, parentRot) = _transformSystem.GetWorldPositionRotation(xform.ParentUid);
                    var targetRot = ToPi(Angle.FromWorldVec(comp.TargetCoordinates.Value - globalPos) - parentRot);
                    var locMin = comp.LocalRot.Theta - comp.AngleBounds.Theta;
                    var locMax = comp.LocalRot.Theta + comp.AngleBounds.Theta;

                    // Makes sure that the min/max are on the same side of the radian discontinuity as the target 
                    if(targetRot < 0 && locMin > 0)
                    {
                        locMin -= Math.Tau;
                        locMax -= Math.Tau;
                    }

                    comp.TargetInBounds = targetRot > locMin && targetRot < locMax;

                    if(comp.TargetInBounds)
                        goalRot = targetRot;
                }
                else
                    comp.TargetInBounds = false;

                if(goalRot == ToPi(xform.LocalRotation))
                    continue;
                
                comp.State = ControlledTurretFiringState.Moving;
                comp.TargetRotation = goalRot;
            }

            if (TryRotate())
            {
                comp.TargetRotation = null;
                comp.State = comp.TargetInBounds ? ControlledTurretFiringState.Ready : ControlledTurretFiringState.Idle;
            }
            continue;

            // A customized version of RotateToFaceSystem.TryRotateTo()
            bool TryRotate()
            {
                // ... something's gone wrong, so we're *definitely* facing the target... yeah
                if (comp.TargetRotation == null)
                    return true;

                var target = comp.TargetRotation.Value;
                var rotationDiff = Angle.ShortestDistance(xform.LocalRotation, target).Theta;
                var maxRotate = MathHelper.DegreesToRadians(comp.RotationSpeed) * frameTime;

                if (Math.Abs(rotationDiff) > maxRotate)
                {
                    var goalTheta = xform.LocalRotation + (Math.Sign(rotationDiff) * maxRotate);
                    _transformSystem.SetLocalRotation(uid, goalTheta, xform);
                    rotationDiff = target - goalTheta;

                    return Math.Abs(rotationDiff) <= comp.AngleTolerance;
                }

                _transformSystem.SetLocalRotation(uid, target, xform);

                return true;
            }
        }
    }

    private static double ToPi(double theta) => ((theta + Math.PI) % (2 * Math.PI)) - Math.PI;
}