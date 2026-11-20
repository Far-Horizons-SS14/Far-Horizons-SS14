using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared._FarHorizons.Medical.Trauma;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.LimbStatusEffects;

public sealed partial class LimbStatusEffectDisableLimb : LimbStatusEffectLocal<LimbStatusEffectDisableLimb>
{
    [ViewVariables] public bool Valid = false;

    protected override bool Eq(LimbStatusEffectDisableLimb other) => Valid == other.Valid;

    protected override LimbStatusEffectDisableLimb Stack(LimbStatusEffectDisableLimb other) => 
        new LimbStatusEffectDisableLimb
        {
            Valid = Valid | other.Valid,
        };

    public override void Normalize()
    {
        if (Limb == null ||
            (Limb.Value != "HandLeft" &&
             Limb.Value != "HandRight"))
            return;
        
        Valid = true;
    }

    public override void Apply(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs)
    {
        if (!Valid ||
            !TryGetTarget(target, organs, out var limb) || 
            entMan.Deleted(limb))
            return;
        
        entMan.EnsureComponent<SurgeryLimbStatusLimbDisabledComponent>(limb);
    }
    public override void Remove(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs)
    {
        if (!Valid ||
            !TryGetTarget(target, organs, out var limb) || 
            entMan.Deleted(limb))
            return;
        
        entMan.RemoveComponentDeferred<SurgeryLimbStatusLimbDisabledComponent>(limb);
    }

    public override void Update(IEntityManager entMan, EntityUid target, ILimbStatusEffect old, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs) => 
        Apply(entMan, target, organs);
}