using Content.Shared._Starlight.Cargo.TamperSeal.Components;

namespace Content.Shared._FarHorizons.Cargo;

public abstract partial class SharedPersonalCargoOrderSystem : EntitySystem
{
    public abstract void UpdateOrderDeliveryStatus(TamperSealValueComponent sealValue, bool failed);
}