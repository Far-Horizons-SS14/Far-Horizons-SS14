using Content.Shared._FarHorizons.PaintableWall;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.PaintableWall;

public sealed class PaintableWallSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearanceSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PaintableWallComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<PaintableWallComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearanceSystem.TryGetData<string>(entity, PaintableVisuals.Prototype, out var prototype, args.Component))
            return;
        
        if (!_prototypeManager.Resolve(prototype, out var target))
            return;

        if (!target.TryGetComponent(out SpriteComponent? targetSprite, _componentFactory))
            return;

        var sprite = (entity.Owner, args.Sprite);

        _sprite.SetBaseRsi(sprite, targetSprite.BaseRSI);
    }
}