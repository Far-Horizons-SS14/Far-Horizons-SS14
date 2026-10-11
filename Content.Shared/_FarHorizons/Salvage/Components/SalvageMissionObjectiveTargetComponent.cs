using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Salvage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SalvageMissionObjectiveTargetComponent : Component
{
    public ProtoId<SalvageMissionObjectivePrototype>? OwnedBy = null;
    
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SecurityIconPrototype> TargetStatusIcon = "SalvageTargetIcon";
}