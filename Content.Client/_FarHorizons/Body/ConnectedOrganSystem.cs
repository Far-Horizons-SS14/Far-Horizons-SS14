using System.Linq;
using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._FarHorizons.Body;

public sealed partial class ConnectedOrganSystem : SharedConnectedOrganSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly MarkingManager _marking = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConnectedOrganComponent, EntInsertedIntoContainerMessage>(OnConnectedOrganAttached);
        SubscribeLocalEvent<ConnectedOrganComponent, EntRemovedFromContainerMessage>(OnConnectedOrganRemoved);

        SubscribeLocalEvent<VisualOrganComponent, OnDisconnectedVisualOrganState>(OnVisualOrganState);
        SubscribeLocalEvent<VisualOrganMarkingsComponent, OnDisconnectedVisualMarkingsOrganState>(OnVisualOrganMarkingsState);
    }

    protected override void OnConnectedOrganInit(Entity<ConnectedOrganComponent> ent, ref ComponentInit args)
    {
        base.OnConnectedOrganInit(ent, ref args);

        if (HasComp<DetachedOrganVisualsExcludedComponent>(ent)) return;

        if (!TryComp<VisualOrganComponent>(ent, out var visualOrgan)) return;

        ApplyVisuals(ent.Owner, (ent.Owner, visualOrgan));

        if (!TryComp<VisualOrganMarkingsComponent>(ent, out var visualMarkingsOrgan)) return;

        ApplyMarkings(ent.Owner, (ent, visualMarkingsOrgan));
    }

    private void OnVisualOrganState(Entity<VisualOrganComponent> ent, ref OnDisconnectedVisualOrganState args)
    {
        if (_container.TryGetContainingContainer(ent.Owner, out var container) && 
            TryComp<ConnectedOrganComponent>(container.Owner, out _))
            ApplyVisuals(container.Owner, ent);
        
        if (HasComp<DetachedOrganVisualsExcludedComponent>(ent)) return;
        
        ApplyVisuals(ent.Owner, ent);
    }

    private void OnVisualOrganMarkingsState(Entity<VisualOrganMarkingsComponent> ent, ref OnDisconnectedVisualMarkingsOrganState args)
    {
        if (_container.TryGetContainingContainer(ent.Owner, out var container) && 
            TryComp<ConnectedOrganComponent>(container.Owner, out _))
            ApplyMarkings(container.Owner, ent);
        
        if (HasComp<DetachedOrganVisualsExcludedComponent>(ent)) return;
        
        ApplyMarkings(ent.Owner, ent);
    }

    private void OnConnectedOrganAttached(Entity<ConnectedOrganComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<VisualOrganComponent>(args.Entity, out var visualOrgan)) return;

        ApplyVisuals(ent.Owner, (args.Entity, visualOrgan));

        if (!TryComp<VisualOrganMarkingsComponent>(args.Entity, out var visualMarkingsOrgan)) return;

        ApplyMarkings(ent.Owner, (args.Entity, visualMarkingsOrgan));
    }

    private void OnConnectedOrganRemoved(Entity<ConnectedOrganComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryComp<VisualOrganComponent>(args.Entity, out var visualOrgan))
            return;

        RemoveVisuals(ent.Owner, (args.Entity, visualOrgan));

        if (!TryComp<VisualOrganMarkingsComponent>(args.Entity, out var visualMarkingsOrgan)) return;

        RemoveMarkings(ent.Owner, (args.Entity, visualMarkingsOrgan));
    }

    private void ApplyVisuals(Entity<SpriteComponent?> parent, Entity<VisualOrganComponent> part)
    {
        if (!Resolve(parent, ref parent.Comp))
            return;

        InitLayers(parent);

        if (!_sprite.LayerMapTryGet(parent, part.Comp.Layer, out var index, false))
        {
            index = _sprite.AddLayer(parent, part.Comp.Data, null);
            _sprite.LayerMapSet(parent, part.Comp.Layer, index);
        }
        else
        {
            _sprite.LayerSetData(parent, index, part.Comp.Data);
        }
    }

    private void RemoveVisuals(Entity<SpriteComponent?> parent, Entity<VisualOrganComponent> part)
    {
        if (!Resolve(parent, ref parent.Comp))
            return;

        if (!_sprite.TryGetLayer(parent, "connectedOrgansMapInitLayer", out _, false)) return; // No sprite changes were aplied

        if (_sprite.LayerMapTryGet(parent, part.Comp.Layer, out var index, false))
            _sprite.RemoveLayer(parent, index, false);
    }

    private void ApplyMarkings(Entity<SpriteComponent?> target, Entity<VisualOrganMarkingsComponent> organ)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        InitLayers(target);
        var unshadedShader = _prototype.Index(SpriteSystem.UnshadedId);
        
        foreach (var markings in organ.Comp.Markings.Values)
        {
            foreach (var marking in markings)
            {
                if (!_marking.TryGetMarking(marking, out var proto))
                    continue;

                if (!_sprite.LayerMapTryGet(target, proto.BodyPart, out var index, false))
                {
                    index = target.Comp.AllLayers.Count();
                    _sprite.AddBlankLayer((target.Owner, target.Comp), index);
                    _sprite.LayerMapSet(target, proto.BodyPart, index);
                }
                
                for (var i = 0; i < proto.Sprites.Count; i++)
                {
                    var sprite = proto.Sprites[i];
                    if (sprite is not SpriteSpecifier.Rsi rsi)
                        continue;

                    var layerId = $"{proto.ID}-{rsi.RsiState}";

                    if (!_sprite.LayerMapTryGet(target, layerId, out _, false))
                    {
                        var layer = _sprite.AddLayer(target, sprite, index + i + 1);
                        _sprite.LayerMapSet(target, layerId, layer);
                        _sprite.LayerSetSprite(target, layerId, rsi);
                    }

                    if (marking.MarkingColors is not null && i < marking.MarkingColors.Count)
                        _sprite.LayerSetColor(target, layerId, marking.MarkingColors[i]);
                    else
                        _sprite.LayerSetColor(target, layerId, Color.White);

                    if (marking.IsGlowing && _sprite.TryGetLayer(target, layerId, out var markingLayer, true))
                    {
                        markingLayer.Shader = unshadedShader.Instance();
                    }
                }
            }
        }
    }

    private void RemoveMarkings(Entity<SpriteComponent?> parent, Entity<VisualOrganMarkingsComponent> part)
    {
        if (!Resolve(parent, ref parent.Comp))
            return;

        if (!_sprite.TryGetLayer(parent, "connectedOrgansMapInitLayer", out _, false)) return; // No sprite changes were aplied

        foreach (var markings in part.Comp.Markings.Values)
        {
            foreach (var marking in markings)
            {
                if (!_marking.TryGetMarking(marking, out var proto))
                    continue;

                if (_sprite.LayerMapTryGet(parent, proto.BodyPart, out var index, false))
                    _sprite.RemoveLayer(parent, index, false);

                foreach (var sprite in proto.Sprites)
                {
                    if (sprite is not SpriteSpecifier.Rsi rsi)
                        continue;

                    var layerId = $"{proto.ID}-{rsi.RsiState}";

                    if (_sprite.LayerMapTryGet(parent, layerId, out index, false))
                        _sprite.RemoveLayer(parent, index, false);
                }
            }
        }
    }

    private void InitLayers(Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp)) return;

        if (_sprite.TryGetLayer(target, "connectedOrgansMapInitLayer", out _, false)) return;

        for (var i = 0; i < target.Comp.AllLayers.Count(); i++) 
            _sprite.RemoveLayer(target, i, out _);

        _sprite.AddBlankLayer((target.Owner, target.Comp));
        _sprite.LayerMapSet(target, "connectedOrgansMapInitLayer", 0);
    }
}