using System.Linq;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Stunnable;
using Robust.Shared.Random;

namespace Content.Shared._FarHorizons.LimbDamage;

public partial class LimbDamageSystem
{

    private void InitHitChance()
    {
        SubscribeLocalEvent<GodmodeComponent, LimbHitCheckEvent>(LimbHitOnGodMode, before: [typeof(BodySystem)]);
        SubscribeLocalEvent<MobStateComponent, LimbHitCheckEvent>(LimbHitOnState, before: [typeof(BodySystem)]);
        SubscribeLocalEvent<KnockedDownComponent, LimbHitCheckEvent>(LimbHitOnKnockdown, before: [typeof(BodySystem)]);
        SubscribeLocalEvent<DamageableLimbComponent, BodyRelayedEvent<LimbHitCheckEvent>>(LimbHitCheck);
        SubscribeLocalEvent<DamageableLimbComponent, BodyRelayedEvent<LimbScatterHitTargetCheckEvent>>(LimbScatterHitCheck);
    }

    private void LimbHitOnGodMode(Entity<GodmodeComponent> ent, ref LimbHitCheckEvent args)
    {
        args.Handled = true;
        args.HitTarget = null; // Always miss on god mode
    }

    private void LimbScatterHitCheck(Entity<DamageableLimbComponent> ent, ref BodyRelayedEvent<LimbScatterHitTargetCheckEvent> args)
    {
        if (args.Args.Handled ||
            !TryComp<OrganComponent>(ent, out var organ) ||
            organ.Category == null)
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var hitChance = args.Args.AimedTowards != organ.Category
            ? ent.Comp.ScatterHitChance
            : ent.Comp.AimedTowardsScatterHitChance;
        if (random.NextFloat() <= hitChance)
            args.Args = args.Args with { Handled = true, Target = organ.Category };
    }

    private void LimbHitOnKnockdown(Entity<KnockedDownComponent> ent, ref LimbHitCheckEvent args)
    {
        var target = args.Target;

        if (args.Handled ||
            !TryComp<BodyComponent>(ent, out var body) ||
            body.Organs == null ||
            !body.Organs.ContainedEntities.Any(p =>
                TryComp<OrganComponent>(p, out var organ) && organ.Category == target))
            return;

        args.Handled = true;
        args.HitTarget = args.Target;
    }

    private void LimbHitOnState(Entity<MobStateComponent> ent, ref LimbHitCheckEvent args)
    {
        var target = args.Target;

        if (ent.Comp.CurrentState < MobState.ActiveCritical ||
            args.Handled ||
            !TryComp<BodyComponent>(ent, out var body) ||
            body.Organs == null ||
            !body.Organs.ContainedEntities.Any(p =>
                TryComp<OrganComponent>(p, out var organ) && organ.Category == target))
            return;

        args.Handled = true;
        args.HitTarget = args.Target;
    }

    private void LimbHitCheck(Entity<DamageableLimbComponent> ent, ref BodyRelayedEvent<LimbHitCheckEvent> args)
    {
        if (args.Args.Handled ||
            !TryComp<OrganComponent>(ent, out var organ) ||
            organ.Category != args.Args.Target)
            return;

        args.Args = args.Args with { Handled = true };
        
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        if (ent.Comp.MissChance > 0 && random.NextFloat() <= ent.Comp.MissChance)
        {
            args.Args = args.Args with { HitTarget = null };
            return;
        }

        if (ent.Comp.RedirectChance > 0 && random.NextFloat() <= ent.Comp.RedirectChance)
        {
            args.Args = args.Args with { HitTarget = ent.Comp.RedirectTarget };
            return;
        }

        args.Args = args.Args with { HitTarget = organ.Category };
    }
}