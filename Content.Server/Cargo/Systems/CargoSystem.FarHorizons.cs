using Content.Server._FarHorizons.Banking;
using Content.Server._Starlight.Cargo.TamperSeal.Components;
using Content.Server.Cargo.Components;
using Content.Server.CartridgeLoader;
using Content.Shared._FarHorizons.Banking;
using Content.Shared._Starlight.Cargo.TamperSeal.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CartridgeLoader;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Systems;
public sealed partial class CargoSystem
{
    [Dependency] private BankingSystem _banking = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private CartridgeLoaderSystem _cartridge = default!;

    private static readonly Color _personalOrderColor = Color.Gray;
    public static readonly ProtoId<CargoAccountPrototype> PersonalAccount = "Cargo"; // Just needs to be put somewhere, money will be charged separately

    public bool TryAddPersonalOrder(EntityUid dbUid, CargoOrderData data, StationCargoOrderDatabaseComponent component) =>
        TryAddOrder(dbUid, PersonalAccount, data, component);
    
    private bool PersonalAccountHasBalance(NetEntity netUid, CargoProductPrototype product)
    {
        var uid = GetEntity(netUid);
        
        if (_banking.GetBalance(uid) is not {} balance)
            return false;
        
        var cost = Math.Floor(product.Cost * product.CreditCost);
        var spendable = BankingSystem.GetRemainingLimit(balance);

        return spendable >= cost;
    }

    private void UpdatePersonalBankAccount(NetEntity netUid, CargoProductPrototype product)
    {
        var uid = GetEntity(netUid);
        
        var cost = (int)MathF.Floor(product.Cost * product.CreditCost);

        _banking.ChangeBalance(uid, -cost);
    }

    private void NotifyAppUser(NetEntity user, bool success = true)
    {
        var ent = GetEntity(user);

        if (!_inventory.TryGetSlotEntity(ent, SharedBankingSystem.PDA_SLOT_NAME, out var pdaEnt) ||
            !TryComp<CartridgeLoaderComponent>(pdaEnt, out var loader))
            return;
        
        var message = success ? Loc.GetString("gsl-now-order-status-success") : Loc.GetString("gsl-now-order-status-cancelled");
        
        _cartridge.SendNotification(pdaEnt.Value, Loc.GetString("gsl-now-order-status-header"), message, loader);
    }

    private string GetPersonalOrderPaperConent(CargoOrderData order, CargoProductPrototype product)
    {
        if (order.PersonalOrderRecipient == null ||
            order.PersonalOrderStation == null ||
            order.PersonalOrderInstructions == null)
            return "";

        return Loc.GetString(
               "cargo-console-personal-order-paper-print-text",
                ("orderNumber", order.OrderId),
                ("itemName", product.Name),
                ("recipient", order.PersonalOrderRecipient!),
                ("station", order.PersonalOrderStation!),
                ("instructions", order.PersonalOrderInstructions!),
                ("approver", string.IsNullOrWhiteSpace(order.Approver) ? Loc.GetString("cargo-console-paper-approver-default") : order.Approver));
    }

    private void SealPersonalOrder(EntityUid item, CargoOrderData order, CargoProductPrototype product, TamperSealableComponent tamperSealable)
    {
        var seal = EnsureComp<TamperSealComponent>(item);
        seal.EntityAccess = order.ChargeCreditsFrom;
        seal.RecipientName = order.PersonalOrderRecipient ?? "";
        seal.RecipientExamineColor = _personalOrderColor;
        seal.Color = _personalOrderColor;
        seal.DestroyToolQualities = new(tamperSealable.DestroyToolQualities);

        var cost = product.Cost * order.OrderQuantity;

        // Attach a tamper seal value component to enable reward/penalty on unseal/destroy.
        var value = EnsureComp<TamperSealValueComponent>(item);
        value.StationId = GetEntity(order.StationId);
        value.Value = cost;
        value.Reward = (int) Math.Floor(_tamperSealRewardMultiplier * cost); // Rewards rounded down.
        value.Penalty = (int) Math.Ceiling(_tamperSealPenaltyMultiplier * cost); // Penalties rounded up.
        value.Refund = (int) Math.Floor(product.Cost * product.CreditCost);
        value.PersonalRefundTarget = order.ChargeCreditsFrom;
        value.OrderId = order.OrderId;

        // Attach an integrity component. This is used by the integrity system to detect repeat tampering.
        var integrity = EnsureComp<TamperSealIntegrityBeaconComponent>(item);
        integrity.StationId = GetEntity(order.StationId);

        DirtyEntity(item);
    }
}