using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Vehicles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RiderComponent : Component
{
    /// <summary>
    /// The vehicle the person is controlling
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Riding;
    
}