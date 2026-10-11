using Content.Shared._FarHorizons.Banking;
using Content.Shared._FarHorizons.Cargo;

namespace Content.Shared._Starlight.Cargo.TamperSeal;

public sealed partial class SharedTamperSealValueSystem
{
    [Dependency] private SharedBankingSystem _banking = default!;
    [Dependency] private SharedPersonalCargoOrderSystem _personalOrders = default!;

    private void RefundPersonalAccount(NetEntity refundTarget, int amount)
    {
        var ent = GetEntity(refundTarget);
        _banking.ChangeBalance(ent, amount);
    }
}