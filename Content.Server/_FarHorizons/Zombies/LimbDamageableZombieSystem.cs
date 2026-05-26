using System.Linq;
using Content.Server._FarHorizons.Body;
using Content.Server.Mobs;
using Content.Shared._FarHorizons.LimbDamage;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared._FarHorizons.Zombies;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Zombies;

public sealed class LimbDamageableZombieSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LimbDamageSystem _limbDamage = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DeathgaspSystem _deathgasp = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    private static readonly ProtoId<OrganCategoryPrototype> _headCategory = "Head";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimbDamageableComponent, EntityZombifiedEvent>(OnZombified);
        SubscribeLocalEvent<ZombieHeadComponent, DamageChangedEvent>(OnZombieHeadDamaged);
        SubscribeLocalEvent<ZombieHeadComponent, OrganGotRemovedEvent>(OnZombieHeadRemoved, before: [ typeof(VitalOrganSystem) ]);
    }

    private void OnZombified(Entity<LimbDamageableComponent> ent, ref EntityZombifiedEvent args)
    {
        var maybeHead = _limbDamage.GetAllDamageable(ent.AsNullable()).Where(p => p.Comp.Organ?.Category == _headCategory).FirstOrNull();

        if (maybeHead is not { } head)
            return;

        EnsureComp<ZombieHeadComponent>(head);
        _mobThreshold.MakeZombieThresholds(ent.Owner);
    }

    private void OnZombieHeadDamaged(Entity<ZombieHeadComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.Empty || !_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<OrganComponent>(ent, out var organ) ||
            organ.Body == null ||
            !_mobState.IsAlive(organ.Body.Value))
            return;

        var damage = _damageable.GetPositiveDamage((ent.Owner, args.Damageable));
        var totalDamage = damage.DamageDict.Values.Sum(p => (float)p);

        if (!(totalDamage >= ent.Comp.DeathAt)) return;

        _deathgasp.Deathgasp(organ.Body.Value);
        _mobState.ChangeMobState(organ.Body.Value, MobState.Dead);
        _mobThreshold.MakeZombieDead(organ.Body.Value);
    }

    private void OnZombieHeadRemoved(Entity<ZombieHeadComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!TryComp<OrganComponent>(ent, out var organ) ||
            organ.Body == null ||
            !_mobState.IsAlive(organ.Body.Value))
            return;

        _deathgasp.Deathgasp(organ.Body.Value);
        _mobState.ChangeMobState(organ.Body.Value, MobState.Dead);
        _mobThreshold.MakeZombieDead(organ.Body.Value);
    }
}