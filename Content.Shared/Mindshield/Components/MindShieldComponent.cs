using Content.Shared.Revolutionary;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield.Components;

/// <summary>
/// If a player has a Mindshield they will get this component to prevent conversion.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Far Horizons - remove restricted access, give AutoGenerateComponentState
public sealed partial class MindShieldComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] // Far Horizons - Make auto networked
    public ProtoId<SecurityIconPrototype> MindShieldStatusIcon = "MindShieldIcon";
}
