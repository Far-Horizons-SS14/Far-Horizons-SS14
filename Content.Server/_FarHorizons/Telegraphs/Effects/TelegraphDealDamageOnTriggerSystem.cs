using Content.Shared._FarHorizons.LimbDamage;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared._FarHorizons.Telegraphs;
using Content.Shared._FarHorizons.Telegraphs.Effects;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Server._FarHorizons.Telegraphs.Effects;

public sealed partial class TelegraphDealDamageOnTriggerSystem : EntitySystem
{
    [Dependency] private TelegraphedAttackSystem _telegraph = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private LimbDamageSystem _limbDamage = default!;

    private const float RANDOM_LIMB_DAMAGE_MULTIPLIER = 2f; // Deal this much more damage to limbs as dealt to torso (damage is randomly split between all available limbs), when dealt randomly

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

            LimbDamageableComponent? limbDamage = null;

            if (ent.Comp.AllLimbs &&
                Resolve(targetEnt, ref limbDamage))
                _limbDamage.ChangeDamageAll((targetEnt, limbDamage), ent.Comp.Damage, ent.Comp.IgnoreResistances);
            else
            if (ent.Comp.RandomLimbs &&
                Resolve(targetEnt, ref limbDamage))
                _limbDamage.ChangeDamageRandom((targetEnt, limbDamage), ent.Comp.Damage * RANDOM_LIMB_DAMAGE_MULTIPLIER, ent.Comp.IgnoreResistances);
        }
    }
}