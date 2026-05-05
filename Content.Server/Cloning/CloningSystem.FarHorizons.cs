using Content.Shared.Inventory;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Cloning;

public sealed partial class CloningSystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> _protogenArmorTag = "ProtogenCybernetics";

    public void CloneProtogenCybernetics(Entity<InventoryComponent?> original, Entity<InventoryComponent?> clone)
    {
        if (!Resolve(original, ref original.Comp) || !Resolve(clone, ref clone.Comp))
            return;
        
        var coords = Transform(clone).Coordinates;

        var slotEnumerator = _inventory.GetSlotEnumerator(original);
        while (slotEnumerator.NextItem(out var item, out var slot))
        {
            if (!_tag.HasTag(item, _protogenArmorTag))
                continue;

            var cloneItem = CopyItem(item, coords);

            if (cloneItem != null && !_inventory.TryEquip(clone, cloneItem.Value, slot.Name, silent: true, inventory: clone.Comp))
                Del(cloneItem); // delete it again if the clone cannot equip it
        }
    }
}