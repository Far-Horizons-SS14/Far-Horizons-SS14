using Robust.Shared.GameStates;
using Robust.Shared.Toolshed.Commands.Values;

namespace Content.Shared._FarHorizons.CyberneticImplanter;

/// <summary>
/// This component is used to mark entites that used to have CyberneticImplanterComponent that destroyed an organ when implanting, this component is used instead of the original
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UsedCyberneticImplanterComponent : Component
{
    /// <summary>
    /// Name of Organ destroyed when implanting
    /// </summary>
    [AutoNetworkedField]
    public string Organ;

    /// <summary>
    /// Name of Species the destroyed organ belonged to
    /// </summary>
    [AutoNetworkedField]
    public string Species = "";
}