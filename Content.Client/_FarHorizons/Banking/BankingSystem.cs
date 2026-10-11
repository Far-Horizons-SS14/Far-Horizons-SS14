using Content.Shared._FarHorizons.Banking;

namespace Content.Client._FarHorizons.Banking;

public sealed partial class BankingSystem : SharedBankingSystem
{
    public override bool ChangeBalance(EntityUid ent, int delta) => true; // Server side only
}