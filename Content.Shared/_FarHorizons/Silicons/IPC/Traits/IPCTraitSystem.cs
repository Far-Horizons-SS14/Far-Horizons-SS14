using Content.Shared.Actions;
using Content.Shared._FarHorizons.UI.BackgroundTraits;
using Content.Shared._FarHorizons.Silicons.IPC.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;

namespace Content.Shared._FarHorizons.IPC.Traits;

public abstract class IPCTraitSystem<T>
    : BackgroundTraitSystem<IPCBrainHolderComponent, T>
    where T : IPCTraitComponent;

public abstract class IPCTraitPassiveTraitSystem<T>
    : BackgroundPassiveTraitSystem<IPCBrainHolderComponent, T>
    where T : IPCPassiveTraitComponent;

public abstract class IPCActionTraitSystem<T, TEvent>
    : BackgroundActionTraitSystem<IPCBrainHolderComponent, T, TEvent>
    where T : IPCActionTraitComponent
    where TEvent : BaseActionEvent;

public abstract class IPCToggleActionTraitSystem<T, TEvent>
    : BackgroundToggleActionTraitSystem<IPCBrainHolderComponent, T, TEvent>
    where T : IPCToggleActionComponent
    where TEvent : InstantActionEvent;

public sealed partial class ModifyBloodstreamTraitSystem : IPCTraitSystem<ModifyBloodstreamTraitComponent>
{
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    protected override void TraitInit(Entity<IPCBrainHolderComponent, ModifyBloodstreamTraitComponent> ent)
    {
        _bloodstream.SetBloodRefreshRate(ent.Owner, ent.Comp2.BloodRefreshRate);
        _bloodstream.SetBloodReductionAmount(ent.Owner, ent.Comp2.BloodReductionAmount);
    }
}

public sealed partial class SetDamageModifierTraitSystem : IPCTraitSystem<SetDamageModifierTraitComponent>
{
    [Dependency] private DamageableSystem _damageable = default!;
    protected override void TraitInit(Entity<IPCBrainHolderComponent, SetDamageModifierTraitComponent> ent)
    {
        if (!TryComp<DamageableComponent>(ent.Owner, out var damageable))
            return;

        if (damageable.DamageModifierSetId is { } current && (current.Id == "IPCWeaker" || current.Id == "IPCRadiation"))
            _damageable.SetDamageModifierSetId(ent.Owner, "IPCRadiationWeaker");
        else
            _damageable.SetDamageModifierSetId(ent.Owner, ent.Comp2.DamageModifierSetId);
    }
}