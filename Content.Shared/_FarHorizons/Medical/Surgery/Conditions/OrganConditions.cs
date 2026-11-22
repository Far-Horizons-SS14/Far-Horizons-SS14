using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Body;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.Conditions;

public sealed partial class SurgeryHasOrgan : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }
    [DataField(required: true)] public ProtoId<OrganCategoryPrototype> Category;

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        var has = false;

        if (entMan.TryGetComponent(target, out BodyComponent? bodyComp) && bodyComp.Organs != null)
        {
            foreach (var ent in bodyComp.Organs.ContainedEntities)
            {
                if (entMan.TryGetComponent(ent, out OrganComponent? organ) && organ.Category == Category)
                {
                    has = true;
                    break;
                }
            }
        }

        return has != Inverse;
    }
}

public sealed partial class SurgeryOrganHeld : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }
    [DataField(required: true)] public ProtoId<OrganCategoryPrototype> Category;

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (tool is not { } held || !entMan.TryGetComponent(held, out OrganComponent? organ))
            return Inverse;

        return (organ.Category == Category) ^ Inverse;
    }
}
public sealed partial class SurgeryEyeDamageGt : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }
    [DataField(required: true)] public int Value;

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (!entMan.TryGetComponent<BlindableComponent>(target, out var blindable))
            return Inverse;
        
        return (blindable.EyeDamage > Value) ^ Inverse;
    }
}
