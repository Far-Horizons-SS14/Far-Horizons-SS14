using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.EntityConditions.Conditions;

/// <summary>
/// Returns true if this entity can take damage and if its total damage is within a specified minimum and maximum.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T,TCon}"/>
public sealed partial class TotalDamageEntityConditionSystem : EntityConditionSystem<DamageableComponent, TotalDamageCondition>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected override void Condition(Entity<DamageableComponent> entity, ref EntityConditionEvent<TotalDamageCondition> args)
    {
        var total = _damageable.GetPositiveDamage(entity).DamageDict.Sum(p => (float)p.Value);
        args.Result = total >= args.Condition.Min && total <= args.Condition.Max;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class TotalDamageCondition : EntityConditionBase<TotalDamageCondition>
{
    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-total-damage",
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()),
            ("min", Min.Float()));
}
