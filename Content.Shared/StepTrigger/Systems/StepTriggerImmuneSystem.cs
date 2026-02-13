using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.StepTrigger.Components;

namespace Content.Shared.StepTrigger.Systems;

public sealed class StepTriggerImmuneSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PreventableStepTriggerComponent, StepTriggerAttemptEvent>(OnStepTriggerClothingAttempt);
        SubscribeLocalEvent<PreventableStepTriggerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnStepTriggerClothingAttempt(Entity<PreventableStepTriggerComponent> ent, ref StepTriggerAttemptEvent args)
    {
        // Far Horizons adding ProtectionKey value so ProtectedFromStepTriggersComponent can be used for more things than just glass shards and boots
        if ((TryComp<ProtectedFromStepTriggersComponent>(args.Tripper, out var protectionComp) && protectionComp.ProtectionKey == ent.Comp.ProtectionKey) || 
            (_inventory.TryGetInventoryEntity<ProtectedFromStepTriggersComponent>(args.Tripper, out var invProtectionItem) && invProtectionItem.Comp!.ProtectionKey == ent.Comp.ProtectionKey))
        {
            args.Cancelled = true;
        }
    }

    private void OnExamined(EntityUid uid, PreventableStepTriggerComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("clothing-required-step-trigger-examine"));
    }
}
