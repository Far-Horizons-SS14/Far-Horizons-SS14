using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.CartridgeLoader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MedTekAnalyzerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionMedTek";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}