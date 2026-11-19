using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared._FarHorizons.Medical.Trauma;

public abstract partial class TraumaSystem
{
    [SubscribeLocalEvent]
    public void OnDamageOnMove(Entity<SurgeryLimbStatusDamageOnMoveComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement ||
            !TryComp<DamageableComponent>(ent, out var damageable) ||
            !TryComp<LimbDamageableComponent>(ent, out var limbDamageable) ||
            _timing.CurTime <= ent.Comp.NextDamage)
            return;
        
        ent.Comp.NextDamage = _timing.CurTime + ent.Comp.DamageRate;

        foreach (var (limb, damage) in ent.Comp.Damage)
        {
            if (limb == TORSO)
            {
                _damage.TryChangeDamage((ent.Owner, damageable), damage, true);
                continue;
            }
            
            _limbDamage.TryChangeLimbDamage((ent.Owner, limbDamageable), limb, damage, out _, true);
        }
    }
        
    [SubscribeLocalEvent]
    public void OnBlindCanSee(Entity<SurgeryLimbStatusBlindComponent> ent, ref CanSeeAttemptEvent args) => 
        args.Cancel();
    
    [SubscribeLocalEvent]
    public void OnDisabledLimbInit(Entity<SurgeryLimbStatusLimbDisabledComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<OrganComponent>(ent, out var organ) ||
            organ.Body == null)
            return;

        if (TryComp<HandOrganComponent>(ent, out var handOrgan))
            _hands.RemoveHand(organ.Body.Value, handOrgan.HandID);
        
        // TODO, something for legs
    }

    [SubscribeLocalEvent]
    public void OnDisabledLimbShutdown(Entity<SurgeryLimbStatusLimbDisabledComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) ||
            !TryComp<OrganComponent>(ent, out var organ) ||
            organ.Body == null ||
            !TryComp<HandsComponent>(organ.Body.Value, out var handComp) ||
            !TryComp<HandOrganComponent>(ent, out var handOrgan))
            return;
        
        _hands.AddHand((organ.Body.Value, handComp), handOrgan.HandID, handOrgan.Data);
    }
}