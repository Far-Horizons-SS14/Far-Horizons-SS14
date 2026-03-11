using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.VisualPickupable;

[RegisterComponent]
public sealed partial class VisualPickupableComponent : Component
{
    [ViewVariables] public EntityUid? ClonedVisuals = null;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PickupableVisualsComponent : Component
{
    [ViewVariables, AutoNetworkedField] public EntityUid? Source = null;
}
