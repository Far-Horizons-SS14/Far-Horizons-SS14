using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.Medical.Surgery;

[Prototype]
public sealed partial class SurgeryStepPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SurgeryStepPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField(required: true)] public string Name = default!;
    [DataField] public TimeSpan BaseTime = TimeSpan.FromSeconds(1);
    [DataField] public ComponentRegistry? Tool = null;
    [DataField] public LocId? Popup = null;
    [DataField] public SpriteSpecifier? Icon = null;
    [DataField] public bool Repeatable = false;
    [DataField] public List<ISurgeryStepCondition> Conditions = new();
    // Split here is mainly for UI purposes. Soft conditions are things like "grab another tool" and hard conditions are "perform another step first"
    [DataField] public List<ISurgeryStepCondition> SoftConditions = new();
    [DataField] public List<ISurgeryStepEffect> Effects = new();
}

[Prototype]
public sealed partial class SurgeryStepPackPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)] public List<ProtoId<SurgeryStepPrototype>> Steps;
    [DataField] public List<ProtoId<SurgeryTutorialPrototype>> Tutorials = new();
}

[ImplicitDataDefinitionForInheritors]
public partial interface ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool);
}

[ImplicitDataDefinitionForInheritors]
public partial interface ISurgeryStepEffect
{
    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool);
}

[Serializable, NetSerializable]
public enum SurgeryStepType
{
    Organic,
    Inorganic
}