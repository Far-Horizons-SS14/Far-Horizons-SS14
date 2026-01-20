using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Storage;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTwinPointerSystem))]
public sealed partial class TwinPointerComponent : Component
{
    
}