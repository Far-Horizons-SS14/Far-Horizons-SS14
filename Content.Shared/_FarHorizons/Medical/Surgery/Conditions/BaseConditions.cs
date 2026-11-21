using System.Linq;
using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Item;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Tools.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.Conditions;

public sealed partial class SurgeryConditionOr : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }
    [DataField(required: true)] public List<ISurgeryStepCondition> Conditions;

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool) =>
        Conditions.Any(c => c.Validate(entMan, limbStatus, target, limb, tool)) != Inverse;
}

public sealed partial class HeldItemNotSurgeryTool : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool) =>
        tool == null ||
        !entMan.GetComponents(tool.Value).OfType<ISurgeryToolComponent>().Any()
            ? !Inverse
            : Inverse;
}

public sealed partial class HeldItemHasComponenent : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }
    [DataField(required: true)] public ComponentRegistry Components;

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        var hasAny = false;

        foreach (var comp in Components.Values)
        {
            if (!entMan.HasComponent(tool, comp.Component.GetType()))
                continue;
            
            hasAny = true;
            break;
        }

        return hasAny ^ Inverse;
    }
}

public sealed partial class HeldItemSize : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }
    [DataField(required: true)] public List<ProtoId<ItemSizePrototype>> AllowedSizes;

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool) => 
        tool is not { } held || !entMan.TryGetComponent(held, out ItemComponent? item) ||
            !AllowedSizes.Contains(item.Size)
            ? Inverse
            : !Inverse;
}

public sealed partial class WeldingToolEnabledCondition : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool) =>
        (entMan.TryGetComponent<WelderComponent>(tool, out var welder) && welder.Enabled) ^ Inverse;
}

public sealed partial class ConditionNever : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool) =>
        Inverse;
}