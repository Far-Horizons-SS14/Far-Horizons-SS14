using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.LimbStatusEffect;

[Prototype]
public sealed partial class LimbStatusEffectPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField] public TimeSpan? DefaultDuration = null;
    [DataField] public bool Visible = false;
    [DataField] public bool Detectable = false;
    [DataField] public List<ProtoId<LimbStatusEffectPrototype>>? Hides;
    [DataField] public bool HidesAll = false;
    [DataField] public LocId? Name;
    [DataField] public LocId? InspectText;
    [DataField] public List<ILimbStatusEffect> Effects = new();
}

[Prototype]
public sealed partial class LimbStatusChainPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<LimbStatusEffectPrototype>> Steps = new();
}