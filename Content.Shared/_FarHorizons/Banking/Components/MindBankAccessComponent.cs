using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Banking.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MindBankAccessComponent : Component
{
    [DataField] public int RoundSpendingLimit = 5000;
    [ViewVariables] public int Spent = 0;
}