using Content.Shared._FarHorizons.Vehicles;
using Robust.Client.GameObjects;

namespace Content.Client._FarHorizons.Vehicles;

public sealed partial class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChanged(EntityUid uid, VehicleComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(VehicleVisuals.VisualState, out var visualStateObject) ||
            visualStateObject is not VehicleVisualState visualState)
        {
            visualState = VehicleVisualState.Normal;
        }
        UpdateAppearance(uid, visualState, component, args.Sprite);
    }

    private void UpdateAppearance(EntityUid uid, VehicleVisualState visualState, VehicleComponent component, SpriteComponent sprite)
    {        
        switch (visualState)
        {
            case VehicleVisualState.Normal:
                SetLayerState(VehicleVisualLayers.Base, component.BaseState, (uid, sprite));
                _sprite.LayerSetAnimationTime((uid, sprite), 0, 0f);
                break;

            case VehicleVisualState.Moving:
                _sprite.LayerSetAutoAnimated((uid, sprite), VehicleVisualLayers.Base, true);
                break;

            case VehicleVisualState.Broken:
                SetLayerState(VehicleVisualLayers.Base, component.BrokenState, (uid, sprite));
                break;
        }
    }

    private void SetLayerState(VehicleVisualLayers layer, string? state, Entity<SpriteComponent> sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        _sprite.LayerSetAutoAnimated(sprite.AsNullable(), layer, false);
        _sprite.LayerSetRsiState(sprite.AsNullable(), layer, state);
    }
}
