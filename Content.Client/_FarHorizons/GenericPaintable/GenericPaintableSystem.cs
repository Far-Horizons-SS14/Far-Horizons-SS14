using Content.Shared._FarHorizons.GenericPaintable;
using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.GenericPaintable;

public sealed class GenericPaintableSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GenericPaintableComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<GenericPaintableComponent, ComponentInit>(InitComp);
    }

    private void InitComp(EntityUid entity, GenericPaintableComponent comp, ComponentInit args)
    {
        //Does this entity have PaintableComponent? if not, why is GenericPaintableComponent here?
        if (!HasComp<PaintableComponent>(entity))
            RemCompDeferred<GenericPaintableComponent>(entity);
    }

    private void OnAppearanceChange(Entity<GenericPaintableComponent> entity, ref AppearanceChangeEvent args)
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