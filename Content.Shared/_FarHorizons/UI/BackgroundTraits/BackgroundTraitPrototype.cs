using Content.Shared._Starlight.Traits.Effects;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.UI.BackgroundTraits;

[Prototype]
public sealed partial class BackgroundTraitPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)] public LocId Name;
    [DataField(required: true)] public LocId Description;
    [DataField(required: true)] public int Cost;
    [DataField] public List<ProtoId<BackgroundTraitPrototype>> Incompatible = new();
    [DataField] public List<BaseTraitEffect> Effects = new();
}