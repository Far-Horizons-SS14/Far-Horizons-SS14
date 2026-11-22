using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.CartridgeLoader;
using Content.Server.Station.Systems;
using Content.Shared._FarHorizons.Cargo;
using Content.Shared._Starlight.Cargo.TamperSeal.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._FarHorizons.Cargo;

public sealed partial class PersonalCargoOrderSystem : SharedPersonalCargoOrderSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private CargoSystem _cargo = default!;
    [Dependency] private AudioSystem _audio = default!;

    public override void UpdateOrderDeliveryStatus(TamperSealValueComponent sealValue, bool failed)
    {
        if (!TryComp<StationCargoOrderDatabaseComponent>(sealValue.StationId, out var orderDb))
            return;
        
        CargoOrderData? order = orderDb.AllOrders.FirstOrDefault(p => p.OrderId == sealValue.OrderId);

        order?.PersonalDeliverySuccess = !failed;
    }

    private void ProcessOrder(Entity<GSLNowAppComponent> ent, EntityUid source, ProtoId<CargoProductPrototype> product, string recipient, string station, string instructions)
    {
        if (_station.GetOwningStation(source) is not {} stationUid ||
            !TryComp<StationCargoOrderDatabaseComponent>(stationUid, out var orderDb) ||
            ent.Comp.NextOrder > _timing.CurTime)
        {
            _audio.PlayPvs(ent.Comp.FailSound, ent);
            return;
        }

        ent.Comp.NextOrder = _timing.CurTime + ent.Comp.OrderCooldown;

        var orderId = ++orderDb.NumOrdersCreated;
        var data = new CargoOrderData(orderId,
                                      product,
                                      1,
                                      recipient,
                                      Loc.GetString("gsl-now-order-reason"),
                                      CargoSystem.PersonalAccount,
                                      GetNetEntity(stationUid),
                                      GetNetEntity(source),
                                      recipient,
                                      station,
                                      instructions);

        if (!_cargo.TryAddPersonalOrder(stationUid, data, orderDb))
        {
            _audio.PlayPvs(ent.Comp.FailSound, ent);
            return;
        }
        
        _audio.PlayPvs(ent.Comp.SuccessSound, ent);

        if (TryComp<CartridgeComponent>(ent, out var cartridge) &&
            cartridge.LoaderUid != null)
            UpdateUi(ent, cartridge.LoaderUid.Value, source, stationUid);
    }

    private List<CargoOrderData> GetOrders(EntityUid actor, EntityUid station)
    {
        var orders = new List<CargoOrderData>();
        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDb))
            return orders;

        var netEnt = GetNetEntity(actor);
        
        orders = orderDb.AllOrders.Where(p => p.ChargeCreditsFrom == netEnt).ToList();
        return orders;
    }

    private void UpdateUi(Entity<GSLNowAppComponent> ent, EntityUid loader, EntityUid actor, EntityUid station)
    {
        var products = ProtoMan.EnumeratePrototypes<CargoProductPrototype>().Where(p => p.PersonalOrder).ToList();

        var marketIds = products.Select(p => p.Group).Distinct().ToList();
        var productIds = products.Select(p => (ProtoId<CargoProductPrototype>)p.ID).ToList();

        var orders = GetOrders(actor, station);

        var state = new GSLNowUiState(ent.Comp.NextOrder, marketIds, productIds, MetaData(actor).EntityName, MetaData(station).EntityName, orders);

        _cartridge.UpdateCartridgeUiState(loader, state);
    }

    [SubscribeLocalEvent]
    private void OnOrderAppOpened(Entity<GSLNowAppComponent> ent, ref CartridgeUiReadyEvent args)
    {
        if (_station.GetOwningStation(args.Actor) is not {} station)
        {
            _cartridge.DeactivateProgram(args.Loader, ent);
            return;
        }

        UpdateUi(ent, args.Loader, args.Actor, station);
    }

    [SubscribeLocalEvent]
    private void OnOrderAppMessage(Entity<GSLNowAppComponent> ent, ref CartridgeMessageEvent args)
    {
        var loader = GetEntity(args.LoaderUid);

        if (!TryComp<PdaComponent>(loader, out var pda) ||
            !TryComp<CartridgeLoaderComponent>(loader, out var cartridgeLoader))
            return;

        if (args is GSLNowOrderMessageEvent order)
            ProcessOrder(ent, args.Actor, order.Product, order.Recipient, order.Station, order.Instructions);
    }
}