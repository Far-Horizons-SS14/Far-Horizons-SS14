using System.Linq;
using Content.Shared._FarHorizons.Body;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.LimbDamage;

public sealed partial class LimbDamageVisualsSystem : VisualizerSystem<LimbDamageVisualsComponent>
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimbDamageVisualsComponent, OrganGotRemovedEvent>(OnOrganGotRemoved);
    }

    private void OnOrganGotRemoved(Entity<LimbDamageVisualsComponent> ent, ref OrganGotRemovedEvent args)
    {
        Entity<SpriteComponent?> self = ent.Owner;
        Entity<SpriteComponent?> body = args.Target;
        if (!Resolve(body, ref body.Comp) ||
            !Resolve(self, ref self.Comp))
            return;

        foreach (var dmgGroup in ent.Comp.DamageOverlayGroups.Keys)
        {
            var layerId = $"damage-{ent.Comp.Layer}-{dmgGroup}";
            if (_sprite.LayerMapTryGet(body, layerId, out _, false))
                _sprite.RemoveLayer(body, layerId);
            
            var selfLayerId = $"damage-{dmgGroup}";
            if (_sprite.LayerMapTryGet(self, selfLayerId, out _, false))
                _sprite.RemoveLayer(self, selfLayerId);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, LimbDamageVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<DamageVisualizerGroupData>(uid, DamageVisualizerKeys.DamageUpdateGroups,
                out var data, args.Component))
            data = new DamageVisualizerGroupData(_damageable.GetDamagePerGroup(uid).Keys.ToList());
        RefreshVisuals((uid, component), data.GroupList);
    }

    private void RefreshVisuals(Entity<LimbDamageVisualsComponent> ent, List<ProtoId<DamageGroupPrototype>> delta)
    {
        if (!TryComp<OrganComponent>(ent, out var organ) ||
            !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        Entity<SpriteComponent> target = (ent, sprite);
        Entity<SpriteComponent>? body = null;
        if (organ.Body != null &&
            TryComp<SpriteComponent>(organ.Body, out var bodySprite))
            body = (organ.Body.Value, bodySprite);

        var connectedOrgan = HasComp<ConnectedOrganComponent>(ent);

        foreach (var dmgGroup in delta)
        {
            if (!ent.Comp.DamageOverlayGroups.TryGetValue(dmgGroup, out var damageSprite)) continue;
            var shouldDraw = DrawState(ent, dmgGroup, out var state);

            if (!connectedOrgan)
                DrawSelfSprite(target, damageSprite, dmgGroup, shouldDraw, state);
            else
                DrawBodySprite(target, ent.Comp.Layer, damageSprite, dmgGroup, shouldDraw, state);

            if (body != null)
                DrawBodySprite(body.Value, ent.Comp.Layer, damageSprite, dmgGroup, shouldDraw, state);
        }
    }

    private void DrawSelfSprite(Entity<SpriteComponent> target,  LimbDamageSpriteState damageSprite, ProtoId<DamageGroupPrototype> dmgGroup, bool shouldDraw,
        string state)
    {
        var layerId = $"damage-{dmgGroup}";
        if (!_sprite.LayerMapTryGet(target.AsNullable(), layerId, out var index, false))
        {
            index = target.Comp.AllLayers.Count();
            _sprite.AddBlankLayer(target, index);
            _sprite.LayerMapSet(target.AsNullable(), layerId, index);
            _sprite.LayerSetRsi(target.AsNullable(), index, damageSprite.Rsi);
            _sprite.LayerSetColor(target.AsNullable(), index, damageSprite.Color);
        }

        _sprite.LayerSetVisible(target.AsNullable(), index, shouldDraw);
        if (shouldDraw)
            _sprite.LayerSetRsiState(target.AsNullable(), index, state);
    }

    private void DrawBodySprite(Entity<SpriteComponent> body, Enum layer, LimbDamageSpriteState damageSprite, ProtoId<DamageGroupPrototype> dmgGroup, bool shouldDraw,
        string state)
    {
        var layerId = $"damage-{layer}-{dmgGroup}";
        if (!_sprite.LayerMapTryGet(body.AsNullable(), layerId, out var index, false))
        {
            if (!_sprite.LayerMapTryGet(body.AsNullable(), layer, out var layerIndex, true))
                return;

            index = layerIndex + 1;
            
            _sprite.AddBlankLayer(body, index);
            _sprite.LayerMapSet(body.AsNullable(), layerId, index);
            _sprite.LayerSetRsi(body.AsNullable(), index, damageSprite.Rsi);
            _sprite.LayerSetColor(body.AsNullable(), index, damageSprite.Color);
        }

        _sprite.LayerSetVisible(body.AsNullable(), index, shouldDraw);
        if (shouldDraw)
            _sprite.LayerSetRsiState(body.AsNullable(), index, state);
    }

    private bool DrawState(Entity<LimbDamageVisualsComponent> ent, ProtoId<DamageGroupPrototype> dmgGroup, out string state)
    {
        state = "";
        if (!_damageable.GetDamagePerGroup(ent.Owner).TryGetValue(dmgGroup, out var currDamage))
            return false;

        var threshold = ent.Comp.Thresholds.Where(p => p <= currDamage).LastOrDefault(0);
        if (threshold == 0)
            return false;

        state = $"{ent.Comp.Layer.ToString()}_{dmgGroup}_{threshold}";
        return true;
    }
}