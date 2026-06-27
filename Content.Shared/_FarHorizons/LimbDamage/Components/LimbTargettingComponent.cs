using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.LimbDamage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class LimbTargettingComponent : Component
{
    [ViewVariables, AutoNetworkedField] public ProtoId<OrganCategoryPrototype> Target = "Torso";
}

[Serializable, NetSerializable]
public sealed class ChangeLimbTargetMessage(ProtoId<OrganCategoryPrototype> target) : EntityEventArgs
{
    public ProtoId<OrganCategoryPrototype> Target = target;
}