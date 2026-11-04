using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.CartridgeLoader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HullSenseAnalyzerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionHullSense";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}