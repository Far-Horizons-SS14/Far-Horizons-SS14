using Content.Server.Power.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Changes battery values depending on the value
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ModifyBatteryLevelEntityEffectSystem : EntityEffectSystem<PowerCellSlotComponent, ModifyBatteryLevel>
{
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private BatterySystem _battery = default!;
    protected override void Effect(Entity<PowerCellSlotComponent> ent, ref EntityEffectEvent<ModifyBatteryLevel> args)
    {
        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var cell) || cell is not { } battery)
            return;

        _battery.ChangeCharge((battery.Owner, battery.Comp), args.Effect.amount * args.Scale);
    }
}
