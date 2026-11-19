using Content.Shared._FarHorizons.Body;
using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Body;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.Medical.Surgery.Effects;

public sealed partial class SurgeryInsertOrgan : ISurgeryStepEffect
{
    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (tool == null ||
            !entMan.TryGetComponent<BodyComponent>(target, out var body) ||
            body.Organs == null)
            return;
        
        var container = entMan.System<SharedContainerSystem>();
        container.Insert(tool.Value, body.Organs);
    }
}

public sealed partial class SurgeryRemoveOrgan : ISurgeryStepEffect
{
    [DataField(required: true)] public ProtoId<OrganCategoryPrototype> Category;

    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (tool == null ||
            !entMan.TryGetComponent<BodyComponent>(target, out var body) ||
            body.Organs == null ||
            body.Organs.ContainedEntities.FirstOrNull(p => entMan.TryGetComponent<OrganComponent>(p, out var organ) && organ.Category == Category) is not {} targetOrgan)
            return;
        
        var container = entMan.System<SharedContainerSystem>();
        container.TryRemoveFromContainer(targetOrgan);
    }
}

public sealed partial class SurgeryInsertCustomOrgan : ISurgeryStepEffect
{
    [DataField(required: true)] public ProtoId<OrganCategoryPrototype> Category;

    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (tool == null ||
            !entMan.TryGetComponent<BodyComponent>(target, out var body) ||
            body.Organs == null)
            return;
        
        var organ = entMan.EnsureComponent<OrganComponent>(tool.Value);
        organ.Category = Category;
        entMan.Dirty(tool.Value, organ);
        entMan.EnsureComponent<CustomOrganComponent>(tool.Value);
        
        var container = entMan.System<SharedContainerSystem>();
        container.Insert(tool.Value, body.Organs);
    }
}

public sealed partial class SurgeryTreatEyeDamage : ISurgeryStepEffect
{
    [DataField(required: true)] public int Amount;

    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (!entMan.TryGetComponent<BlindableComponent>(target, out var blindable))
            return;
        
        var blind = entMan.System<BlindableSystem>();

        blind.AdjustEyeDamage((target, blindable), Amount * -1);
    }
}
