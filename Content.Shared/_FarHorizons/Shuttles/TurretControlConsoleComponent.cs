using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Shuttles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TurretControlConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public NetEntity? ControlServer;

    [DataField]
    public ProtoId<SinkPortPrototype> LinkingPort = "TurretControlConsoleDataReceiver";
}