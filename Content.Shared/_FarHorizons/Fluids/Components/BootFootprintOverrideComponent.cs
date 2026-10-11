using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Fluids.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BootFootprintOverrideComponent : Component
{
    [DataField(required: true)] public ProtoId<FootprintTypePrototype> Footprint = default!;
}

[ByRefEvent]
public record struct BootFootprintModifyEvent(ProtoId<FootprintTypePrototype> Footprint) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.FEET;
}