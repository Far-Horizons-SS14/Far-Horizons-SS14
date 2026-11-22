using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery;

[Prototype]
public sealed partial class SurgeryTutorialPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)] public string Name = default!;
    [DataField] public List<SurgeryStepTutorial> Steps = new();
    [DataField] public List<ISurgeryStepCondition> Conditions = new();
    [DataField] public List<ProtoId<SurgeryTutorialPrototype>> Finishers = new();
}

[DataDefinition]
public sealed partial class SurgeryStepTutorial
{
    [DataField(required: true)] public ProtoId<SurgeryStepPrototype> Step = default!;
    [DataField] public List<ISurgeryStepCondition> ConditionComplete = new();
}