using System.Linq;
using Content.Shared._FarHorizons.LimbDamage;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

public sealed partial class RepairableSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly LimbDamageSystem _limbDamage = default!; // Far Horizons

    public override void Initialize()
    {
        SubscribeLocalEvent<RepairableComponent, InteractUsingEvent>(Repair);
        SubscribeLocalEvent<RepairableComponent, RepairDoAfterEvent>(OnRepairDoAfter);
    }

    private void OnRepairDoAfter(Entity<RepairableComponent> ent, ref RepairDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(ent.Owner, out DamageableComponent? damageable))
            return;
        
        // Far Horizons start
        var target = args.TargettedLimb;
        FixedPoint2 totalDamage;
        if (target != null && _limbDamage.TryGetFullBodyDamage(ent.Owner) is { } fullBodyDamage &&
            fullBodyDamage.TryGetValue(target.Value, out var limbDamage))
            totalDamage = limbDamage.Sum(p => (float)p.Value);
        else
        {
            totalDamage = _damageableSystem.GetTotalDamage((ent.Owner, damageable));
            target = null;
        }
        // Far Horizons end

        if (target == null && totalDamage == 0)
            return;

        if (ent.Comp.DamageValue != null)
            RepairSomeDamage((ent, damageable), ent.Comp.DamageValue.Value, args.User);
        else if (ent.Comp.Damage != null)
            RepairSomeDamage((ent, damageable), ent.Comp.Damage, args.User, target);
        else
            RepairAllDamage((ent, damageable), args.User);

        args.Repeat = ent.Comp.AutoDoAfter && totalDamage > 0;
        args.Args.Event.Repeat = args.Repeat;
        args.Handled = true;

        if (!args.Repeat)
        {
            var str = Loc.GetString("comp-repairable-repair", ("target", ent.Owner), ("tool", args.Used!));
            _popup.PopupClient(str, ent.Owner, args.User);

            var ev = new RepairedEvent(ent, args.User);
            RaiseLocalEvent(ent.Owner, ref ev);
        }
    }

    /// <summary>
    /// Repairs some damage of a entity.
    /// The healed amount will be evenly distributed among all damage types the entity has.
    /// If one of the damage types of the entity is too low. it will heal that completly and distribute the excess healing among the other damage types
    /// </summary>
    /// <param name="ent">entity to be repaired</param>
    /// <param name="damageAmount">how much damage to repair (value have to be negative to repair)</param>
    /// <param name="user">who is doing the repair</param>
    private void RepairSomeDamage(Entity<DamageableComponent?> ent, float damageAmount, EntityUid user)
    {
        _limbDamage.HealAllEvenly(ent.Owner, damageAmount, origin: user);  // Far Horizons
        var damageChanged = _damageableSystem.HealEvenly(ent.Owner, damageAmount, origin: user);
        _adminLogger.Add(LogType.Healed, $"{ToPrettyString(user):user} repaired {ToPrettyString(ent.Owner):target} by {damageChanged.GetTotal()}");
    }

    /// <summary>
    /// Repairs some damage of a entity
    /// </summary>
    /// <param name="ent">entity to be repaired</param>
    /// <param name="damageAmount">how much damage to repair (values have to be negative to repair)</param>
    /// <param name="user">who is doing the repair</param>
    private void RepairSomeDamage(Entity<DamageableComponent?> ent, Damage.DamageSpecifier damageAmount, EntityUid user, ProtoId<OrganCategoryPrototype>? limbTarget = null)
    {
        // Far Horizons start
        DamageSpecifier healed = new();

        var healingDoneToLimb = false;
        if (limbTarget != null)
        {
            healingDoneToLimb = _limbDamage.TryChangeLimbDamage(ent.Owner, limbTarget.Value,
                damageAmount, out healed, true, false, origin: user);
        }

        if (!healingDoneToLimb)
            _damageableSystem.TryChangeDamage(ent.Owner, damageAmount, out healed, true, false, origin: user);
        // Far Horizons end
        
        _adminLogger.Add(LogType.Healed, $"{ToPrettyString(user):user} repaired {ToPrettyString(ent.Owner):target} by {healed.GetTotal()}");
    }

    /// <summary>
    /// Repairs all damage of a entity
    /// </summary>
    /// <param name="ent">entity to be repaired</param>
    /// <param name="user">who is doing the repair</param>
    private void RepairAllDamage(Entity<DamageableComponent?> ent, EntityUid user)
    {
        _limbDamage.ClearAllDamage(ent.Owner); // Far Horizons
        _damageableSystem.ClearAllDamage(ent);
        _adminLogger.Add(LogType.Healed, $"{ToPrettyString(user):user} repaired {ToPrettyString(ent.Owner):target} back to full health");
    }

    private void Repair(Entity<RepairableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Far Horizons start
        ProtoId<OrganCategoryPrototype>? limbTarget = null;
        var shouldHealLimb = false;
        if (TryComp<LimbDamageableComponent>(ent.Owner, out var limbDamageable))
        {
            limbTarget = _limbDamage.GetCurrentValidTarget((ent.Owner, limbDamageable), args.User);
            var fullBodyDamage = _limbDamage.TryGetFullBodyDamage((ent.Owner, null, limbDamageable));
            shouldHealLimb = limbTarget != null &&
                             fullBodyDamage != null &&
                             fullBodyDamage.TryGetValue(limbTarget.Value, out var limbDamage) &&
                             limbDamage.Sum(p => (float)p.Value) > 0;
        }

        // Only try repair the target if it is damaged
        if (!shouldHealLimb && _damageableSystem.GetTotalDamage(ent.Owner) == 0)
            return;
        // Far Horizons end

        #region Starlight
        var canRepair = new CanRepairEvent();
        RaiseLocalEvent(ent.Owner, ref canRepair);
        if (canRepair.Cancelled)
        {
            _popup.PopupEntity(canRepair.Message, ent.Owner);
            return;
        }
        #endregion

        float delay = ent.Comp.DoAfterDelay;

        // Add a penalty to how long it takes if the user is repairing itself
        if (args.User == args.Target)
        {
            if (!ent.Comp.AllowSelfRepair)
                return;

            delay *= ent.Comp.SelfRepairPenalty;
        }

        // Run the repairing doafter
        args.Handled = _toolSystem.UseTool(args.Used, args.User, ent.Owner, delay, ent.Comp.QualityNeeded, new RepairDoAfterEvent(limbTarget), ent.Comp.FuelCost);
    }
}

/// <summary>
/// Event raised on an entity when its successfully repaired.
/// </summary>
/// <param name="Ent"></param>
/// <param name="User"></param>
[ByRefEvent]
public readonly record struct RepairedEvent(Entity<RepairableComponent> Ent, EntityUid User);

/// <summary>
/// Do after event started when you try to fix a entity with RepairableComponent.
/// This doafter is repeated if the entity has <see cref="AutoDoAfter"> set to true and not all damage was fixed yet.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RepairDoAfterEvent : SimpleDoAfterEvent
{
    public ProtoId<OrganCategoryPrototype>? TargettedLimb; // Far Horizons

    public RepairDoAfterEvent(ProtoId<OrganCategoryPrototype>? limb = null) => TargettedLimb = limb; // Far Horizons
}

#region Starlight

[ByRefEvent]
public sealed class CanRepairEvent : CancellableEntityEventArgs
{
    public string Message = "";
};
#endregion
