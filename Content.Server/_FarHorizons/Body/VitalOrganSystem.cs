using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;

namespace Content.Server._FarHorizons.Body;

public sealed partial class VitalOrganSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VitalOrganComponent, OrganGotRemovedEvent>(OnVitalOrganRemoved);
    }

    private void OnVitalOrganRemoved(Entity<VitalOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;
        if (!TryComp<DamageableComponent>(args.Target, out var damageable)) return;
        if (!TryComp<MobStateComponent>(args.Target, out var mobState) || mobState.CurrentState == Shared.Mobs.MobState.Dead) return;
        
        _damage.ChangeDamage((args.Target, damageable), ent.Comp.Damage, true);
    }
}