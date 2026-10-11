using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Banking.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CredstickATMComponent : Component
{
    [DataField(required: true)] public SoundSpecifier WithdrawSound = default!;
    [DataField(required: true)] public SoundSpecifier ErrorSound = default!;
    [DataField(required: true)] public Dictionary<int, CredstickATMOffering> CredstickOffering = default!;
}

[Serializable, NetSerializable]
public enum CredstickATMUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CredstickATMBuiState : BoundUserInterfaceState
{
    public BankAccountBalance Balance { get; init; }

    public CredstickATMBuiState(BankAccountBalance balance) => 
        Balance = balance;
}

[Serializable, NetSerializable]
public sealed class CredstickATMRequestWithdraw(int amount) : BoundUserInterfaceMessage
{
    public int Amount = amount;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class CredstickATMOffering
{
    [DataField(required: true)] public EntProtoId Proto = default!;
    [DataField(required: true)] public int Cost = default!;
}