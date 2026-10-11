using Content.Shared._FarHorizons.Cargo;
using Content.Shared._Starlight.Cargo.TamperSeal.Components;

namespace Content.Client._FarHorizons.Cargo;

public sealed partial class PersonalCargoOrderSystem : SharedPersonalCargoOrderSystem
{
    public override void UpdateOrderDeliveryStatus(TamperSealValueComponent sealValue, bool failed) {}
}