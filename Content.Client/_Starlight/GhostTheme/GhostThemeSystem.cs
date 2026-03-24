using Content.Shared.Starlight.GhostTheme;
using Content.Shared.Starlight;
using Content.Shared.Ghost;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Content.Server.Administration.Systems;

namespace Content.Client.Starlight.GhostTheme;

public sealed class GhostThemeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly StarlightEntitySystem _entities = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostThemeComponent, AppearanceChangeEvent>(OnAppearance);
    }

    private void OnAppearance(Entity<GhostThemeComponent> ent, ref AppearanceChangeEvent args) 
    {
        var spriteType = _entities.Entity<SpriteComponent>(ent.Owner);

        if (!_appearance.TryGetData<string>(ent.Owner, GhostVisuals.Theme, out var Theme)
            || !_appearance.TryGetData<Color>(ent.Owner, GhostVisuals.Color, out var Color)
            || !_prototypeManager.TryIndex<GhostThemePrototype>(Theme, out var ghostThemePrototype)
            || (Theme == "None" && Color == Color.White)) // Far Horizons
        {
            SetSkin(spriteType, false); // Far Horizons
            return;
        }

        SetSkin(spriteType, true); // Far Horizons
        var layer = _sprite.LayerMapReserve(spriteType, GhostVisuals.Theme);
        _sprite.LayerSetSprite(spriteType, layer, ghostThemePrototype.SpriteSpecifier.Sprite);
        _sprite.LayerSetColor(spriteType, layer, Color != Color.White ? Color : ghostThemePrototype.SpriteSpecifier.SpriteColor);
        _sprite.LayerSetScale(spriteType, layer, ghostThemePrototype.SpriteSpecifier.SpriteScale);
        _sprite.SetDrawDepth(spriteType, DrawDepth.Default + 11);
        spriteType.Comp?.LayerSetShader(layer, "unshaded");

        if(spriteType.Comp == null)
            return;

        spriteType.Comp.NoRotation = ghostThemePrototype.SpriteSpecifier.SpriteRotation;
        spriteType.Comp.OverrideContainerOcclusion = true;
    }

    // Far Horizons
    private void SetSkin(Entity<SpriteComponent?> ent, bool enabled)
    {
        var themeLayer = _sprite.LayerMapReserve(ent, GhostVisuals.Theme);
        var damageLayer = _sprite.LayerMapReserve(ent, GhostVisuals.Damage);
        _sprite.LayerSetVisible(ent, themeLayer, enabled);
        _sprite.LayerSetVisible(ent, damageLayer, !enabled);
    }
}