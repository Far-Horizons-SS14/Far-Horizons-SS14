using Content.Shared.CartridgeLoader;
using Content.Shared.Mind;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Banking.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FrontierBankAppComponent : Component
{
    [ViewVariables] public string? OwnerName;
    [ViewVariables] public BankAccountBalance? Balance;
}

[Serializable, NetSerializable]
public sealed class FrontierBankCredsticDepositMessageEvent(int amount) : CartridgeMessageEvent
{
    public int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class FrontierBankCredsticWithdrawMessageEvent(int amount) : CartridgeMessageEvent
{
    public int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class FrontierBankUiState : BoundUserInterfaceState
{
    public string? OwnerName;
    public BankAccountBalance Balance;
    public int? CredstickBalance;

    public FrontierBankUiState(Entity<MindComponent> owner, BankAccountBalance balance, int? credstickBalance = null)
    {
        OwnerName = owner.Comp.CharacterName;
        Balance = balance;
        CredstickBalance = credstickBalance;
    }

    public FrontierBankUiState(string? ownerName, BankAccountBalance balance, int? credstickBalance = null)
    {
        OwnerName = ownerName;
        Balance = balance;
        CredstickBalance = credstickBalance;
    }
}