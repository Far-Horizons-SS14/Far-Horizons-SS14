using System.Linq;
using System.Numerics;
using Content.Server._FarHorizons.Telegraphs;
using Content.Server.Body.Systems;
using Content.Shared._FarHorizons.Bosses;
using Content.Shared._FarHorizons.Telegraphs;
using Content.Shared._FarHorizons.Telegraphs.Effects;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Bosses.Elder;

public sealed partial class ElderBossSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BossCombatSystem _boss = default!;
    [Dependency] private TelegraphedAttackSystem _telegraph = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private MapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelegraphElderBossHealingDecoyComponent, OnTelegraphAttackFinished>(OnHealingDecoyCast);
        SubscribeLocalEvent<ElderBossDecoyComponent, DamageModifyEvent>(OnDecoyDamaged);
        SubscribeLocalEvent<ElderBossDecoyRegenerationComponent, DamageModifyEvent>(OnBossDamaged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ElderBossDecoyRegenerationComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var comp, out var damageable))
        {
            if (_timing.CurTime < comp.NextUpdate) continue;
            comp.NextUpdate = _timing.CurTime + comp.HealingRate;

            _damage.TryChangeDamage((uid, damageable), comp.Healing);
        }
    }

    private void OnHealingDecoyCast(Entity<TelegraphElderBossHealingDecoyComponent> ent, ref OnTelegraphAttackFinished args)
    {
        var xform = Transform(ent);

        if (xform.GridUid == null ||
            !TryComp<TelegraphedAttackComponent>(ent, out var telegraphComp) ||
            !telegraphComp.IgnoreTargets.Any() ||
            !TryComp<MapGridComponent>(xform.GridUid, out var gridComp) ||
            _telegraph.GetTelegraph((ent, telegraphComp), args.Telegraph) is not {} telegraph ||
            !_transform.TryGetGridTilePosition((ent, xform), out var baseCoords))
            return;
        
        var boss = telegraphComp.IgnoreTargets.First();

        if (!TryComp<BossCombatComponent>(boss, out var bossCombat))
            return;

        var halfTile = gridComp.TileSize / 2f;
        var availableTiles = telegraph.Tiles.ToList();

        var spawnedDecoys = new List<EntityUid>();
        var gridTiles = _map.GetAllTiles(xform.GridUid.Value, gridComp);

        for (var i = 0; i < ent.Comp.NumDecoys; i++)
        {
            Vector2i tile;
            do
            {
                if (!availableTiles.Any()) return;
                tile = _random.PickAndTake(availableTiles);
            } while (!gridTiles.Any(p => p.X == baseCoords.X + tile.X && p.Y == baseCoords.Y + tile.Y));
            var coords = new Vector2(baseCoords.X + tile.X + halfTile, baseCoords.Y + tile.Y + halfTile);
            var mapCoords = new EntityCoordinates(xform.GridUid.Value, coords);
            
            var spawned = SpawnAttachedTo(ent.Comp.Decoy, mapCoords);

            if (!TryComp<ElderBossDecoyComponent>(spawned, out var decoy))
            {
                Del(spawned);
                return;
            }

            decoy.Boss = boss;
            spawnedDecoys.Add(spawned);
            foreach (var attack in bossCombat.Attacks)
                if (TryComp<TelegraphedAttackComponent>(attack, out var attackTelegraph))
                    attackTelegraph.IgnoreTargets.Add(spawned);
        }

        var teleportTile = _random.PickAndTake(availableTiles);
        var teleportCoords = new Vector2(baseCoords.X + teleportTile.X + halfTile, baseCoords.Y + teleportTile.Y + halfTile);
        var teleportMapCoords = new EntityCoordinates(xform.GridUid.Value, teleportCoords);
        _transform.SetCoordinates(boss, teleportMapCoords);

        _bloodstream.TryModifyBloodLevel(boss, 300);
        _bloodstream.TryModifyBleedAmount(boss, -100);
        var regenComp = EnsureComp<ElderBossDecoyRegenerationComponent>(boss);
        regenComp.Healing = ent.Comp.PassiveHealing;
        regenComp.HealingRate = ent.Comp.PassiveHealingRate;
        regenComp.Decoys = spawnedDecoys;
        _boss.SetPaused(boss, true);
    }

    private void OnDecoyDamaged(Entity<ElderBossDecoyComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.Exploding || ent.Comp.Boss == null) return;
        _damage.TryChangeDamage(ent.Comp.Boss.Value, ent.Comp.BossHealing);
        ExplodeDecoy(ent.AsNullable());
    }

    private void OnBossDamaged(Entity<ElderBossDecoyRegenerationComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive()) return;
        foreach (var decoy in ent.Comp.Decoys)
            ExplodeDecoy(decoy);
        _boss.SetPaused(ent.Owner, false);
        RemCompDeferred<ElderBossDecoyRegenerationComponent>(ent);
    }

    private void ExplodeDecoy(Entity<ElderBossDecoyComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp) ||
            ent.Comp.Exploding)
            return;

        ent.Comp.Exploding = true;

        var spawned = _telegraph.SpawnTelegraphAt(ent.Comp.ExplosionTelegraph, ent);

        if (!TryComp<TelegraphDeleteEntityOnTriggerComponent>(spawned, out var deleteComp))
            Del(ent);
        
        deleteComp!.Entity = ent;
    }
}