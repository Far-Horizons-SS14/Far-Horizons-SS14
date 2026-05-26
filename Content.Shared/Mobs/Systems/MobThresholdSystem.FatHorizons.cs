using Content.Shared.Mobs.Components;

namespace Content.Shared.Mobs.Systems;

public sealed partial class MobThresholdSystem
{
    public void MakeZombieThresholds(Entity<MobThresholdsComponent?> mob)
    {
        if (!Resolve(mob, ref mob.Comp))
            return;

        mob.Comp.Thresholds = [];
        mob.Comp.Thresholds.Add(0, MobState.Alive);
        Dirty(mob);
    }

    public void MakeZombieDead(Entity<MobThresholdsComponent?> mob)
    {
        if (!Resolve(mob, ref mob.Comp))
            return;

        mob.Comp.Thresholds = [];
        mob.Comp.Thresholds.Add(0, MobState.Dead);
        Dirty(mob);
    }
}