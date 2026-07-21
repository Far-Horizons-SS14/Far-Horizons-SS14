using Content.Shared._FarHorizons.LimbDamage;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared._FarHorizons.Telegraphs;
using Content.Shared._FarHorizons.Telegraphs.Effects;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Server._FarHorizons.Telegraphs.Effects;

public sealed partial class TelegraphDealDamageOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly TelegraphedAttackSystem _telegraph = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly LimbDamageSystem _limbDamage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelegraphDealDamageOnTriggerComponent, OnTelegraphTriggered>(OnTriggered);
    }

    private void OnTriggered(Entity<TelegraphDealDamageOnTriggerComponent> ent, ref OnTelegraphTriggered args)
    {
        if (!TryComp<TelegraphedAttackComponent>(ent, out var telegraphComp) ||
            _telegraph.GetTelegraph((ent, telegraphComp), args.Telegraph) is not {} telegraph)
            return;
        
        foreach (var targetEnt in _telegraph.FindAffectedComponents<DamageableComponent>((ent, telegraphComp), telegraph))
        {
            _damage.TryChangeDamage(targetEnt.AsNullable(), ent.Comp.Damage, ent.Comp.IgnoreResistances);

            if (ent.Comp.AllLimbs &&
                TryComp<LimbDamageableComponent>(targetEnt, out var limbDamage))
                _limbDamage.ChangeDamageAll((targetEnt, limbDamage), ent.Comp.Damage, ent.Comp.IgnoreResistances);
        }
    }
}