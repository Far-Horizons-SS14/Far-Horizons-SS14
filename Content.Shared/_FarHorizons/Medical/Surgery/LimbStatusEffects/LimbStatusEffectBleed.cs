using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared._FarHorizons.Medical.Trauma;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.LimbStatusEffects;

public sealed partial class LimbStatusEffectBleed : LimbStatusEffectGlobal<LimbStatusEffectBleed>
{
    [DataField(required: true)] public int MinBleedCap;

    protected override bool Eq(LimbStatusEffectBleed other) => MinBleedCap == other.MinBleedCap;

    protected override LimbStatusEffectBleed Stack(LimbStatusEffectBleed other)
    {
        var result = new LimbStatusEffectBleed
        {
            Limb = Limb,
            MinBleedCap = Math.Max(MinBleedCap, other.MinBleedCap)
        };

        return result;
    }

    public override void Apply(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs)
    {
        var comp = entMan.EnsureComponent<SurgeryLimbStatusMinCapBleedComponent>(target);
        comp.MinBleedCap = MinBleedCap;
        entMan.Dirty(target, comp);
    }
    public override void Remove(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs) =>
        entMan.RemoveComponentDeferred<SurgeryLimbStatusMinCapBleedComponent>(target);

    public override void Update(IEntityManager entMan, EntityUid target, ILimbStatusEffect old, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs) => 
        Apply(entMan, target, organs);
}