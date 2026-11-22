using System.Linq;
using System.Numerics;
using Content.Shared._FarHorizons.Fluids;
using Content.Shared._FarHorizons.Fluids.Components;
using Content.Shared.GameTicking;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._FarHorizons.Fluids;

public sealed partial class FluidFootprintSystem : SharedFluidFootprintSystem
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TransformSystem _transform = default!;
    

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(_ => ClearCache());
    }

    [SubscribeLocalEvent]
    private void OnContainerState(Entity<FluidFootprintContainerComponent> ent, ref AfterAutoHandleStateEvent args) =>
        UpdateSprite(ent);

    protected override void UpdateSprite(Entity<FluidFootprintContainerComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;
        
        var entityRotation = _transform.GetWorldRotation(ent);
        
        while (sprite.AllLayers.Count() < ent.Comp.Footprints.Count)
        {
            var packedPrint = ent.Comp.Footprints[sprite.AllLayers.Count()];
            var print = ent.Comp.Unpack(packedPrint);
            var proto = _protoMan.Index(print.Footprint);

            var layerKey = _sprite.AddLayer((ent, sprite), new SpriteSpecifier.Rsi(proto.RsiPath, proto.RsiState));
            var scale = new Vector2(print.Size, print.Size);

            if (print.Flip)
                scale.X = -scale.X;

            
            _sprite.LayerSetScale((ent, sprite), layerKey, scale);
            _sprite.LayerSetColor((ent, sprite), layerKey, print.Color.WithAlpha(print.Opacity));
            _sprite.LayerSetOffset((ent, sprite), layerKey, print.Position);
            var angle = print.Angle - entityRotation - Angle.FromDegrees(90);
            _sprite.LayerSetRotation((ent, sprite), layerKey, angle);
        }
    }
}