using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server._FarHorizons.Body;

public sealed partial class OrganWithInventorySlotsSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganWithInventorySlotsComponent, OrganGotRemovedEvent>(OnOrganRemoved);
        SubscribeLocalEvent<NeedsOrgansForInventorySlotsComponent, IsEquippingTargetAttemptEvent>(OnTryEquip);
    }

    private void OnTryEquip(Entity<NeedsOrgansForInventorySlotsComponent> ent, ref IsEquippingTargetAttemptEvent args)
    {
        if (!ent.Comp.Slots.Contains(args.Slot) ||
            !TryComp<BodyComponent>(ent, out var body) ||
            body.Organs == null ||
            body.Organs.Count == 0)
            return;

        foreach (var organ in body.Organs.ContainedEntities)
            if (TryComp<OrganWithInventorySlotsComponent>(organ, out var organInventory) &&
                organInventory.Slots.Contains(args.Slot))
                return;
        
        args.Cancel();
    }

    private void OnOrganRemoved(Entity<OrganWithInventorySlotsComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!TryComp<OrganComponent>(ent, out var organ) || organ.Body == null ||
            TerminatingOrDeleted(organ.Body.Value)) return;
        foreach (var slot in ent.Comp.Slots)
            _inventory.TryUnequip(organ.Body.Value, slot, true, true);

    }
}