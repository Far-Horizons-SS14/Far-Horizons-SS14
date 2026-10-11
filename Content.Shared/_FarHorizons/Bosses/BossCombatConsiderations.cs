using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._FarHorizons.Bosses;

public interface IBossMechanicConsideration
{
    bool Consider(IEntityManager entMan, Entity<BossCombatComponent> ent);
}

[DataDefinition]
public sealed partial class HealthInRangeConsideration : IBossMechanicConsideration
{
    [DataField] public float? MinHealth;
    [DataField] public float? MaxHealth;

    public bool Consider(IEntityManager entMan, Entity<BossCombatComponent> ent)
    {
        if (!entMan.TryGetComponent<DamageableComponent>(ent, out var damageableComp) ||
            !entMan.TryGetComponent<MobThresholdsComponent>(ent, out var thresholdComp))
            return false;
        
        var damageable = entMan.System<DamageableSystem>();
        var totalDamage = damageable.GetPositiveDamage((ent, damageableComp)).DamageDict.Sum(p => (float)p.Value);

        var mobThreshold = entMan.System<MobThresholdSystem>();
        var deadThreshold = (float)mobThreshold.GetThresholdForState(ent, MobState.Dead, thresholdComp);

        var ratio = 1 - (totalDamage / deadThreshold);

        return (MinHealth == null || !(ratio < MinHealth)) && 
               (MaxHealth == null || !(ratio > MaxHealth));
    }
}