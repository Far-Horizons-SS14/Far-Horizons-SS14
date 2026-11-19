using System.Linq;
using Content.Shared.Body;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Medical.LimbStatusEffect;

public sealed partial class LimbStatusEffectSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _netMan = default!;

    private static readonly ProtoId<OrganCategoryPrototype> _defaultLimb = "Torso";

    [SubscribeLocalEvent]
    public void OnOrganInserted(Entity<LimbStatusEffectManagerComponent> ent, ref OrganInsertedIntoEvent args) =>
        RefreshState(ent.AsNullable());
    
    [SubscribeLocalEvent]
    public void OnOrganRemoved(Entity<LimbStatusEffectManagerComponent> ent, ref OrganRemovedFromEvent args) =>
        RefreshState(ent.AsNullable());

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        List<EntityUid>? toDelete = null;
        List<ProtoId<LimbStatusEffectPrototype>>? expired = null;

        var query = EntityQueryEnumerator<LimbStatusEffectComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            expired?.Clear();

            foreach (var (id, expiry) in comp.Statuses)
            {
                if (expiry == null || expiry > curTime)
                    continue;

                expired ??= [];
                expired.Add(id);
            }

            if (expired != null)
                foreach (var id in expired)
                    RemoveStatusEffect((uid, comp), id);

            if (comp.Statuses.Count == 0)
            {
                toDelete ??= [];
                toDelete.Add(uid);
            }
        }

        if (toDelete == null)
            return;

        foreach (var deleting in toDelete)
        {
            RemComp<LimbStatusEffectComponent>(deleting);
            RefreshStatus(deleting);
        }
    }

    private void RefreshStatus(EntityUid uid)
    {
        if (HasComp<BodyComponent>(uid))
            RefreshState(uid);
        else if (TryComp<OrganComponent>(uid, out var organ) &&
                 organ.Body != null)
            RefreshState(organ.Body.Value);
    }

    private void RefreshState(Entity<LimbStatusEffectManagerComponent?> ent)
    {
        if (_netMan.IsClient || // There is no point in going through headache, effects are generally not predicted as player causing effect and player seeing it are 2 different players
            TerminatingOrDeleted(ent) ||
            !Resolve(ent, ref ent.Comp))
            return;
        
        // Collect all effects

        Dictionary<ProtoId<OrganCategoryPrototype>, List<ILimbStatusEffect>> allEffects = new();

        if (TryComp<LimbStatusEffectComponent>(ent, out var bodyEffects) &&
            GetStatusEffects((ent, bodyEffects)) is {} bodyStatuses &&
            bodyStatuses.Any())
            allEffects[_defaultLimb] = [.. bodyStatuses.Select(p => ProtoMan.Index(p)).SelectMany(p => p.Effects)];
        
        var organ_cache = new Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid>();

        if (TryComp<BodyComponent>(ent, out var body) &&
            body.Organs != null)
        {
            foreach (var organ in body.Organs.ContainedEntities)
            {
                if (!TryComp<OrganComponent>(organ, out var organComp) ||
                    organComp.Category == null)
                    continue;
                
                organ_cache[organComp.Category.Value] = organ;

                if (!TryComp<LimbStatusEffectComponent>(organ, out var organEffects) ||
                    GetStatusEffects((organ, organEffects)) is not {} organStatuses ||
                    !organStatuses.Any())
                    continue;
                
                allEffects[organComp.Category.Value] = [.. organStatuses.Select(p => ProtoMan.Index(p)).SelectMany(p => p.Effects)];
            }
        }
        
        // Copy and sort them for stacking

        Dictionary<string, List<ILimbStatusEffect>> stackingState = new();
        foreach(var (organ, effects) in allEffects)
        {
            foreach(var effect in effects)
            {
                var copy = effect.Clone();
                copy.Limb = organ;
                copy.Normalize();

                if (copy.Id() is not {} id)
                    continue;

                if (!stackingState.TryGetValue(id, out var stackingList))
                {
                    stackingList = new List<ILimbStatusEffect>();
                    stackingState[id] = stackingList;
                }

                stackingList.Add(copy);
            }
        }

        // Stack effects

        Dictionary<string, ILimbStatusEffect> desiredState = new();
        foreach (var (id, list) in stackingState)
        {
            if (!list.Any())
                continue;

            var stacked = list[0];
            for (var i = 1; i < list.Count; i++)
                stacked = stacked.Stack(list[i]);
            
            desiredState[id] = stacked;
        }

        // Remove/Update existing effects

        foreach (var (id, effect) in ent.Comp.LastAppliedState)
        {
            if (!desiredState.Keys.Contains(id))
                effect.Remove(EntityManager, ent, organ_cache);
            else if (!effect.Eq(desiredState[id]))
                desiredState[id].Update(EntityManager, ent, effect, organ_cache);
        }

        // Apply new effects

        foreach (var (id, effect) in desiredState)
        {
            if (ent.Comp.LastAppliedState.Keys.Contains(id))
                continue;
            
            effect.Apply(EntityManager, ent, organ_cache);
        }

        ent.Comp.LastAppliedState = desiredState;
    }
}