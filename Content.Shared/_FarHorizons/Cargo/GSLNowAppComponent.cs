using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CartridgeLoader;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Cargo;

[RegisterComponent, NetworkedComponent]
public sealed partial class GSLNowAppComponent : Component
{
    [DataField(required: true)] public TimeSpan OrderCooldown;
    [ViewVariables] public TimeSpan NextOrder;
    [DataField(required: true)] public SoundSpecifier FailSound;
    [DataField(required: true)] public SoundSpecifier SuccessSound;
}

[Serializable, NetSerializable]
public sealed class GSLNowUiState(TimeSpan nextOrder, List<ProtoId<CargoMarketPrototype>> markets, List<ProtoId<CargoProductPrototype>> products, string deliveryName, string deliveryStation, List<CargoOrderData> orders) : BoundUserInterfaceState
{
    public TimeSpan NextOrder = nextOrder;
    public List<ProtoId<CargoProductPrototype>> Products = products;
    public List<ProtoId<CargoMarketPrototype>> Markets = markets;
    public string DeliveryName = deliveryName;
    public string DeliveryStation = deliveryStation;
    public List<CargoOrderData> Orders = orders;
}

[Serializable, NetSerializable]
public sealed class GSLNowOrderMessageEvent(ProtoId<CargoProductPrototype> product, string recipient, string station, string instructions) : CartridgeMessageEvent
{
    public ProtoId<CargoProductPrototype> Product = product;
    public string Recipient = recipient;
    public string Station = station;
    public string Instructions = instructions;
}