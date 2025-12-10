using Content.Shared._FarHorizons.Vehicles;
using Content.Shared._FarHorizons.Vehicles.Components;
using Content.Shared._FarHorizons.Vehicles.EntitySystems;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._FarHorizons.Vehicles;

public sealed class VehicleSystems : SharedVehicleSystems
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, VehicleComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
   
        if (!_sprite.TryGetLayer((uid, args.Sprite), VehicleVisualLayers.AutoAnimate, out var _, false))
            return;

        var state = component.BaseState;
        var drawDepth = DrawDepth.BelowMobs;

        _sprite.LayerSetRsiState((uid, args.Sprite), VehicleVisualLayers.AutoAnimate, state);
        _sprite.SetDrawDepth((uid, args.Sprite), (int)drawDepth);

        if (component.AutoAnimate && TryComp<SpriteComponent>(uid, out var spriteComp))
            _sprite.LayerSetAutoAnimated((uid, spriteComp), VehicleVisualLayers.AutoAnimate, true);
    }
}
