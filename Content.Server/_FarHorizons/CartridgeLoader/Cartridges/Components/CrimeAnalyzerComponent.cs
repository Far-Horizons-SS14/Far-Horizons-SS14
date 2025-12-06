using Robust.Shared.Prototypes;
namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class CrimeAnalyzerComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionCrimeCheck";

    [DataField]
    public EntityUid? ActionEntity;
}
