using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.LimbDamage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LimbAimedThrowComponent : Component
{
    [DataField] public ProtoId<OrganCategoryPrototype> Target = "Torso";
}