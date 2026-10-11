using System.Numerics;
using Content.Shared._FarHorizons.VFX;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.VFX;

public sealed class BossVFXOverlay : Overlay
{
    private readonly IEntityManager _entMan;
    private readonly IPrototypeManager _protoMan;
    private readonly EntityLookupSystem _lookup;
    private readonly SharedTransformSystem _transform;

    private readonly HashSet<Entity<TransformComponent, OverlayVFXComponent>> _visualizers = new();

     public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public BossVFXOverlay(IEntityManager entMan, IPrototypeManager protoMan)
    {
        _entMan = entMan;
        _protoMan = protoMan;
        _lookup = _entMan.System<EntityLookupSystem>();
        _transform = _entMan.System<SharedTransformSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _visualizers.Clear();
        _lookup.GetEntitiesOnMap(args.MapId, _visualizers);

        return _visualizers.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var viewport = args.WorldBounds.CalcBoundingBox();

        handle.SetTransform(Matrix3x2.Identity);

        foreach (var (uid, xform, vfx) in _visualizers)
        {
            if (string.IsNullOrEmpty(vfx.Shader) ||
                !_protoMan.TryIndex<ShaderPrototype>(vfx.Shader, out var shaderProto))
                continue;

            var worldPos = _transform.GetWorldPosition(xform);

            var shader = shaderProto.Instance().Duplicate();
            shader.SetParameter("center", worldPos);
            shader.SetParameter("rotation", (float) xform.LocalRotation.Theta);
            shader.SetParameter("viewportMin", viewport.BottomLeft);
            shader.SetParameter("viewportMax", viewport.TopRight);
            shader.MakeImmutable();
            handle.UseShader(shader);

            handle.DrawRect(viewport, Color.White);
        }

        handle.UseShader(null);
    }
}