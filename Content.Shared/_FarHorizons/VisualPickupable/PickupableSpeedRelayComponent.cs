using Content.Shared.Inventory;

namespace Content.Shared._FarHorizons.VisualPickupable;

[RegisterComponent]
public sealed partial class PickupableSpeedRelayComponent : Component;

[ByRefEvent]
public record struct PickupableArmorSpeedRelayEvent(float WalkSpeedModifier, float SprintSpeedModifier)
    : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.OUTERCLOTHING;
}