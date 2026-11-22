using Content.Shared.Clumsy;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Kitchen.Components;
using Content.Shared.Projectiles;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Shared.Execution;

public sealed partial class SharedExecutionSystem
{
    private const float MeleeDoAfterDuration = 5f;

    [SubscribeLocalEvent]
    private void OnGetMeleeVerb(EntityUid uid, SharpComponent _, GetVerbsEvent<UtilityVerb> args)
    {
        if (HasComp<ExecutionComponent>(uid)
            || args.Hands == null
            || args.Using == null
            || !args.CanAccess
            || !args.CanInteract
            || !CanExecuteWithMelee(args.Using.Value, args.Target, args.User))
        {
            return;
        }

        args.Verbs.Add(new UtilityVerb
        {
            Act = () => StartMeleeExecution(args.Using.Value, args.Target, args.User),
            Impact = LogImpact.High,
            Text = args.User == args.Target ? Loc.GetString("suicide-verb-name") : Loc.GetString("execution-verb-name"),
            Message = args.User == args.Target ? Loc.GetString("suicide-verb-message") : Loc.GetString("execution-verb-message"),
        });
    }

    private bool CanExecuteWithMelee(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        return CanBeExecuted(victim, user)
            && TryComp<MeleeWeaponComponent>(weapon, out var melee)
            && melee.Damage.GetTotal() > 0f;
    }

    private void StartMeleeExecution(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithMelee(weapon, victim, attacker))
            return;

        var self = attacker == victim;
        ShowExecutionInternalPopup(
            self ? "execution-popup-self-melee-initial-internal" : "execution-popup-melee-initial-internal",
            attacker,
            victim,
            weapon);
        ShowExecutionExternalPopup(
            self ? "execution-popup-self-melee-initial-external" : "execution-popup-melee-initial-external",
            attacker,
            victim,
            weapon);

        _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            attacker,
            MeleeDoAfterDuration,
            new MeleeExecutionDoAfterEvent(),
            weapon,
            target: victim,
            used: weapon)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    [SubscribeLocalEvent]
    private void OnMeleeExecution(Entity<SharpComponent> ent, ref MeleeExecutionDoAfterEvent args)
    {
        if (args.Handled
            || args.Cancelled
            || args.Used is not { } weapon
            || args.Target is not { } victim
            || !TryComp<MeleeWeaponComponent>(weapon, out var melee)
            || !CanExecuteWithMelee(weapon, victim, args.User))
        {
            return;
        }

        if (args.User == victim)
        {
            if (!TryComp<DamageableComponent>(victim, out var damageable))
                return;

            _audio.PlayPredicted(melee.HitSound, victim, victim);
            _suicide.ApplyLethalDamage((victim, damageable), melee.Damage);
            ShowExecutionInternalPopup("execution-popup-self-melee-complete-internal", victim, victim, weapon, false);
            ShowExecutionExternalPopup("execution-popup-self-melee-complete-external", victim, victim, weapon);
        }
        else
        {
            _damageable.TryChangeDamage(victim, melee.Damage * DamageMultiplier, true);
            ShowExecutionInternalPopup("execution-popup-melee-complete-internal", args.User, victim, weapon);
            ShowExecutionExternalPopup("execution-popup-melee-complete-external", args.User, victim, weapon);
        }

        args.Handled = true;
    }
}
