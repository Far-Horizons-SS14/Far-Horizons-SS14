using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Banking.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CredstickTransferSenderComponent : Component
{
    [ViewVariables, AutoNetworkedField] public EntityUid? FromCredstick = null;
    [ViewVariables, AutoNetworkedField] public EntityUid? ToCredstick = null;
    [ViewVariables, AutoNetworkedField] public EntityUid? FromEntity = null;
    [ViewVariables, AutoNetworkedField] public EntityUid? ToEntity = null;
    [ViewVariables] public int? Amount = null;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CredstickTransferReceiverComponent : Component
{
    [ViewVariables, AutoNetworkedField] public EntityUid? FromCredstick = null;
    [ViewVariables, AutoNetworkedField] public EntityUid? ToCredstick = null;
    [ViewVariables, AutoNetworkedField] public EntityUid? FromEntity = null;
    [ViewVariables, AutoNetworkedField] public EntityUid? ToEntity = null;
    [ViewVariables] public int? Amount = null;
}

[RegisterComponent]
public sealed partial class CredstickDirectTransferTargetComponent : Component;

[Serializable, NetSerializable]
public enum CredstickTransferUi : byte
{
    DialogKey,
}

[Serializable, NetSerializable]
public sealed class CredstickTransferDialogConfirmed(int amount) : BoundUserInterfaceMessage
{
    public int Amount = amount;
}

public sealed partial class CredstickTransferAlertEvent : BaseAlertEvent;