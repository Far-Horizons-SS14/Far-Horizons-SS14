using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Movement;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(BodySpeedModifierSystem))]
public sealed partial class BodySpeedModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public float WalkModifier = 1.0f;

    [DataField, AutoNetworkedField]
    public float SprintModifier = 1.0f;
}