using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Body;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTargetComponent : Component
{
    [DataField] public List<ProtoId<SurgeryStepPackPrototype>> Steps = new();
    [DataField] public List<SlotFlags> BlockingSlots = new();
    [DataField] public SurgeryStepType SurgeryType = SurgeryStepType.Organic;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryLimbTargetComponent : Component
{
    [DataField] public List<ProtoId<SurgeryStepPackPrototype>> Steps = new();
    [DataField] public List<SlotFlags> BlockingSlots = new();
    [DataField] public SurgeryStepType SurgeryType = SurgeryStepType.Organic;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class NecrosisSurgeryTargetComponent : Component
{
    [DataField] public Dictionary<ProtoId<OrganCategoryPrototype>, List<ProtoId<LimbStatusEffectPrototype>>> NecrosisTarget = new();
    [DataField] public TimeSpan NecrosisStatusCost = TimeSpan.FromMinutes(1);
    [DataField] public Dictionary<ProtoId<LimbStatusEffectPrototype>, ProtoId<OrganCategoryPrototype>> StatusOrganDependency = new();
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryBedSpeedComponent : Component
{
    [DataField]
    public float BedSpeedModifier = 2.0f;
}
