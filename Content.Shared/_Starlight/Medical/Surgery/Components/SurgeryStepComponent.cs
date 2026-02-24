using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
namespace Content.Shared.Starlight.Medical.Surgery.Steps;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSurgerySystem))]
[EntityCategory("SurgerySteps")]
public sealed partial class SurgeryStepComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Duration = 2;

    [DataField]
    public ComponentRegistry? Tools;

    [DataField]
    public ComponentRegistry? Add;

    [DataField]
    public ComponentRegistry? BodyAdd;

    [DataField]
    public ComponentRegistry? Remove;

    [DataField]
    public ComponentRegistry? BodyRemove;
    //FarHorizons Start
    [DataField]
    public bool Repeatable = false;

    [DataField] public ProtoId<OrganCategoryPrototype>? OrganCategory; // If Tools is set to "Organ" type - only this category of organ will be considered
    //FarHorizons End

    [DataField]
    public ProtoId<ReagentPrototype>? ReagentId = null;

    [DataField]
    public FixedPoint2 ReagentQuantity = FixedPoint2.New(5);
}