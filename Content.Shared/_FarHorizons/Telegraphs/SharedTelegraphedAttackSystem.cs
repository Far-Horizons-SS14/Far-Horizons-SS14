using System.Linq;
using Content.Shared._FarHorizons.Bosses;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Telegraphs;

public abstract partial class SharedTelegraphedAttackSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly List<(EntProtoId, EntityCoordinates, List<EntityUid>)> _spawnQueue = [];

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TelegraphedAttackComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.IsActive)
                continue;
            
            Entity<TelegraphedAttackComponent> ent = (uid, comp);
            
            if (comp.DeleteOnComplete && Timing.CurTime >= comp.StartTime + GetTotalDuration(ent))
            {
                var lastId = comp.Telegraphs.Select((val, id) => (val, id)).OrderBy(p => p.val.EffectDelay).Last().id;
                ProcessTelegraph(ent, lastId, true);
                PredictedDel(uid);
                continue;
            }

            var considered = comp.Telegraphs
                             .Select((val, id) => (val, id))
                             .Where(p => !comp.TriggeredParts.Contains(p.id))
                             .Where(p => Timing.CurTime >= comp.StartTime + p.val.DisplayDelay);

            foreach (var (_, id) in considered)
            {
                if (ProcessTelegraph(ent, id))
                    comp.TriggeredParts.Add(id);
            }
        }

        // A bit weird, but we can't spawn attacks from events caused by attacks, as query.MoveNext() from like above will complain about collection being modified. So this is a workaround queue
        foreach (var (proto, coords, ignored) in _spawnQueue)
        {
            var spawned = SpawnAttachedTo(proto, coords);
            if (!TryComp<TelegraphedAttackComponent>(spawned, out var telegraph))
                continue;
            telegraph.IgnoreTargets.AddRange(ignored);

            if (!telegraph.IgnoreTargets.Any()) continue;

            var boss = telegraph.IgnoreTargets.First();
            if (TryComp<BossCombatComponent>(boss, out var bossCombat))
                bossCombat.Attacks.Add(spawned);
        }
        _spawnQueue.Clear();
    }

    private bool ProcessTelegraph(Entity<TelegraphedAttackComponent> ent, int telegraphId, bool finished = false)
    {
        if (ent.Comp.Telegraphs.ElementAt(telegraphId) is not {} telegraph)
            return false;

        if (ent.Comp.TriggeredParts.Contains(telegraphId))
            return false;

        // Entity will be deleted after this, skip the checks and call the events
        if (finished)
        {
            TriggerTelegraph(ent, telegraphId, finished);
            return true;
        }

        // Has the final tick of this telegraph happen?
        if (Timing.CurTime >= ent.Comp.StartTime + telegraph.EffectDelay)
        {
            TriggerTelegraph(ent, telegraphId);
            return true;
        }

        // Continuous telegraphs tick their effects periodically 
        if (ent.Comp.Continuous)
        {
            if (Timing.CurTime < ent.Comp.ContinuousEffectNextRefresh)
                return false;
            ent.Comp.ContinuousEffectNextRefresh = Timing.CurTime + ent.Comp.ContinuousEffectRefreshRate;

            TriggerTelegraph(ent, telegraphId);
        }

        return false;
    }

    private void TriggerTelegraph(Entity<TelegraphedAttackComponent> ent, int telegraphId, bool finished = false)
    {
        var ev = new OnTelegraphTriggered(telegraphId);
        RaiseLocalEvent(ent, ref ev);
        
        if (finished)
        {
            var finishEv = new OnTelegraphAttackFinished(telegraphId);
            RaiseLocalEvent(ent, ref finishEv);
            return;
        }
    }

    public void DelayedSpawnAttack(EntProtoId proto, EntityCoordinates mapCoords, List<EntityUid> ignored) => 
        _spawnQueue.Add((proto, mapCoords, ignored));
}