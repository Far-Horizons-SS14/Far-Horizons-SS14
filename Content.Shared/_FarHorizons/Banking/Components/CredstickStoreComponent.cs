using Content.Shared.Containers.ItemSlots;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Banking.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CredstickStoreComponent : Component
{
    [DataField(required: true)] public ProtoId<CurrencyPrototype> Currency = default!;
    [DataField(required: true)] public ItemSlot CredstickSlot = default!;
    [DataField] public string SlotId = "credstick-slot";
}