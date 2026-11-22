using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Banking.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CredstickComponent : Component
{
    [DataField, AutoNetworkedField] public int Balance = 0;
    [DataField] public Enum TransferUiKey = CredstickTransferUi.DialogKey;
    [ViewVariables(VVAccess.ReadOnly)] public EntityUid? TransferSource;
    [ViewVariables(VVAccess.ReadOnly)] public EntityUid? TransferTarget;
}

[Serializable, NetSerializable]
public enum CredstickContentsState : byte
{
    Empty
}
