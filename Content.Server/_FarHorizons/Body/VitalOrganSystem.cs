using System.Linq;
using Content.Server.Mobs;
using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Body;

public sealed partial class VitalOrganSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private DeathgaspSystem _deathGasp = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DelayedVitalOrganDeathComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var delayedDeath, out var damageable))
        {
            if (delayedDeath.DieAt == null ||
                delayedDeath.Damage == null ||
                delayedDeath.DieAt.Value > _timing.CurTime)
                continue;
            
            _deathGasp.Deathgasp(uid);
            _damage.ChangeDamage((uid, damageable), delayedDeath.Damage, true);
            RemCompDeferred<DelayedVitalOrganDeathComponent>(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnVitalOrganRemoved(Entity<VitalOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;
        if (!TryComp<DamageableComponent>(args.Target, out var damageable) ||
            !TryComp<OrganComponent>(ent, out var organ) ||
            organ.Category == null)
            return;
        if (!TryComp<MobStateComponent>(args.Target, out var mobState) || mobState.CurrentState == Shared.Mobs.MobState.Dead) return;
        
        if (ent.Comp.KillDelay == null)
        {
            _deathGasp.Deathgasp(args.Target);
            _damage.ChangeDamage((args.Target, damageable), ent.Comp.Damage, true);
            return;
        }

        var delayedDeath = EnsureComp<DelayedVitalOrganDeathComponent>(args.Target);
        delayedDeath.DieAt = _timing.CurTime + ent.Comp.KillDelay.Value;
        delayedDeath.Damage = ent.Comp.Damage;
        delayedDeath.CausedBy.Append(organ.Category.Value);
    }

    [SubscribeLocalEvent]
    private void OnDelayedDeathPrevented(Entity<DelayedVitalOrganDeathComponent> ent, ref OrganInsertedIntoEvent args)
    {
        if (!TryComp<OrganComponent>(args.Organ, out var organ) ||
            organ.Category == null)
            return;
        
        ent.Comp.CausedBy.Remove(organ.Category.Value);

        if (ent.Comp.CausedBy.Any())
            return;
        
        RemCompDeferred<DelayedVitalOrganDeathComponent>(ent);
    }
}