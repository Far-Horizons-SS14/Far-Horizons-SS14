using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.PDA;

public sealed partial class PdaComponent
{
    public const string PdaCredstickSlotId = "PDA-credstick";
    [DataField] public ItemSlot CredstickSlot = new();
}