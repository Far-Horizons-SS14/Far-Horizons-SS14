using System.Numerics;
using Content.Shared._FarHorizons.VisualPickupable;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._FarHorizons.VisualPickupable;

public sealed class VisualPickupableSystem : SharedVisualPickupableSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static Vector2 _offset = new Vector2(0, 0.1f);

    private HashSet<Entity<PickupableVisualsComponent>> _trackedEntities = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickupableVisualsComponent, AfterAutoHandleStateEvent>((ent, ref _) => UpdateState(ent));
        SubscribeLocalEvent<PickupableVisualsComponent, MapInitEvent>((ent, ref _) => UpdateState(ent));
    }

    private void UpdateState(Entity<PickupableVisualsComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out _))
            return;

        if (ent.Comp.Source == null)
            _trackedEntities.Remove(ent);
        else if (TryComp<SpriteComponent>(ent.Comp.Source, out _))
            _trackedEntities.Add(ent);
    }

    public override void FrameUpdate(float frameTime)
    {
        var spriteQuery = GetEntityQuery<SpriteComponent>();
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var transformQuery = GetEntityQuery<TransformComponent>();
        foreach (var ent in _trackedEntities)
        {
            if (!spriteQuery.TryGetComponent(ent, out var targetSprite) ||
                !spriteQuery.TryGetComponent(ent.Comp.Source, out var sourceSprite) ||
                (metaQuery.GetComponent(ent).Flags & MetaDataFlags.Detached) != 0)
                continue;

            _sprite.CopySprite((ent.Comp.Source.Value, sourceSprite), (ent, targetSprite));
            _sprite.SetRotation((ent, targetSprite), Angle.FromDegrees(90));

            if (!transformQuery.TryGetComponent(ent.Comp.Source, out var sourceTransform) ||
                !transformQuery.TryGetComponent(sourceTransform.ParentUid, out var parentTransform)) return;

            // It's very stupid and 100% won't cause any problems in the future:
            // If facing north relative to screen - move sprite up so it's drawn behind the carrying character
            // If it's facing not north - move sprite down to draw it on top
            var facingDirection = _transform.GetWorldRotation(parentTransform) + _eyeManager.CurrentEye.Rotation;

            if (facingDirection.GetCardinalDir() == Direction.North)
                _sprite.SetOffset((ent, targetSprite), _offset);
            else
                _sprite.SetOffset((ent, targetSprite), -_offset);
        }
    }
}