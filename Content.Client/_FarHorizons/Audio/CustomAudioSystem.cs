using System.Collections.Concurrent;
using System.Numerics;
using Content.Shared._FarHorizons.Audio;
using Content.Shared._FarHorizons.CCVar;
using Content.Shared.Doors.Components;
using Content.Shared.Physics;
using Content.Shared.Tools.Components;
using Robust.Client.Audio;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Client._FarHorizons.Audio.CustomAudio;

public sealed partial class CustomAudioSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IConfigurationManager _cfgManager = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const float OcclusionReferenceThickness = 1f;
    private readonly ConcurrentDictionary<EntityUid, float> _smoothedOcclusion = new();

    // CVAR Controlled Values
    private float _maxRayLength;
    private float _muffleDecayConstant;
    private float _maxOcclusion;
    private float _occlusionSmoothingRate;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfgManager, CVars.AudioRaycastLength, OnRaycastLengthChanged, true);
        Subs.CVar(_cfgManager, FHCCVars.AudioOcclusionMuffleDecay, OnMuffleDecayChanged, true);
        Subs.CVar(_cfgManager, FHCCVars.AudioOcclusionMax, OnMaxOcclusionChanged, true);
        Subs.CVar(_cfgManager, FHCCVars.AudioOcclusionSmoothingRate, OnSmoothingRateChanged, true);

        _audio.GetOcclusionOverride += GetOcclusion;
        SubscribeLocalEvent<EntityTerminatingEvent>(OnEntityTerminating);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _audio.GetOcclusionOverride -= GetOcclusion;
    }

    private void OnEntityTerminating(ref EntityTerminatingEvent ev)
        => _smoothedOcclusion.Remove(ev.Entity, out _);

    private void OnRaycastLengthChanged(float value)
        => _maxRayLength = value;

    private void OnMuffleDecayChanged(float value)
        => _muffleDecayConstant = value;

    private void OnMaxOcclusionChanged(float value)
        => _maxOcclusion = value;

    private void OnSmoothingRateChanged(float value)
        => _occlusionSmoothingRate = value;

    private float GetOcclusion(MapCoordinates listener, Vector2 delta, float distance, EntityUid? ignoredEnt)
    {
        if (distance <= 0.1f)
            return 0f;

        var rayLength = MathF.Min(distance, _maxRayLength);
        var direction = delta / distance;
        var ray = new CollisionRay(listener.Position, direction, (int) (CollisionGroup.Opaque | CollisionGroup.Impassable));

        var results = _physics.IntersectRay(listener.MapId, ray, rayLength, ignoredEnt, returnOnFirstHit: false);

        var totalOcclusion = 0f;
        foreach (var result in results)
        {
            if (!TryComp<StructureOcclusionComponent>(result.HitEntity, out var occlusionComp))
                continue;

            if (!occlusionComp.DoesOcclusionWorkWhenOpen
                && TryComp<DoorComponent>(result.HitEntity, out var doorComp)
                && doorComp.State is DoorState.Open or DoorState.Opening)
                continue;

            var aabb = _lookup.GetWorldAABB(result.HitEntity);
            if (!TryGetRayAabbPenetration(listener.Position, direction, aabb, rayLength, out var penetration))
                continue;

            var occlusion = occlusionComp.OcclusionAmount
                * Math.Clamp(penetration / OcclusionReferenceThickness, 0f, 1f);

            if (TryComp<WeldableComponent>(result.HitEntity, out var weldable) && weldable.IsWelded)
                occlusion *= occlusionComp.WeldedOcclusionModifier;

            totalOcclusion += occlusion;
        }

        var target = _maxOcclusion * (1f - MathF.Exp(-_muffleDecayConstant * totalOcclusion));

        return SmoothOcclusion(ignoredEnt, target);
    }

    private float SmoothOcclusion(EntityUid? source, float target)
    {
        if (source is not { } key)
            return target;

        var dt = (float) _timing.FrameTime.TotalSeconds;
        var maxDelta = _occlusionSmoothingRate * dt;
        
        return _smoothedOcclusion.AddOrUpdate(
        key,
        target,
        (_, current) =>
        {
            var diff = target - current;
            return MathF.Abs(diff) <= maxDelta
                ? target
                : current + (MathF.Sign(diff) * maxDelta);
        });
    }

    private static bool TryGetRayAabbPenetration(Vector2 origin, Vector2 dir, Box2 aabb, float rayLength, out float penetration)
    {
        penetration = 0f;

        var tMin = 0f;
        var tMax = rayLength;

        for (var axis = 0; axis < 2; axis++)
        {
            var o = axis == 0 ? origin.X : origin.Y;
            var d = axis == 0 ? dir.X : dir.Y;
            var min = axis == 0 ? aabb.Left : aabb.Bottom;
            var max = axis == 0 ? aabb.Right : aabb.Top;

            if (MathF.Abs(d) < 1e-6f)
            {
                if (o < min || o > max)
                    return false;
                continue;
            }

            var invD = 1f / d;
            var t1 = (min - o) * invD;
            var t2 = (max - o) * invD;

            if (t1 > t2)
                (t1, t2) = (t2, t1);

            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);

            if (tMin > tMax)
                return false;
        }

        penetration = MathF.Max(0f, tMax - tMin);
        return penetration > 0f;
    }
}