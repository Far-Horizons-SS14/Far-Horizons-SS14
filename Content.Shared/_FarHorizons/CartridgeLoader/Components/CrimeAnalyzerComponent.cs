using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared._FarHorizons.CartridgeLoader;

[RegisterComponent, NetworkedComponent]
public sealed partial class CrimeAnalyzerComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionCrimeCheck";

    [DataField]
    public EntityUid? ActionEntity;
}
