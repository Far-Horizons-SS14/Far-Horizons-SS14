using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Disease.Components;

/// <summary>
/// Component for entities that can diagnose diseases.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DiseaseDiagnoserComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId PaperPrototype = "DiagnosisReportPaper";
}
