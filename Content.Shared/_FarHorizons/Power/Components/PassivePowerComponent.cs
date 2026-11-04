using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.Components;

/// <summary>
/// Provides a local power state for entities that do not connect to an APC network.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PassivePowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Powered = true;
}
