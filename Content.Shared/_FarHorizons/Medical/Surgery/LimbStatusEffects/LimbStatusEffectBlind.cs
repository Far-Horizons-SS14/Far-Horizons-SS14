using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared._FarHorizons.Medical.Trauma;
using Content.Shared.Body;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.LimbStatusEffects;

public sealed partial class LimbStatusEffectBlind : LimbStatusEffectGlobal<LimbStatusEffectBlind>
{
    protected override bool Eq(LimbStatusEffectBlind other) => true;

    protected override LimbStatusEffectBlind Stack(LimbStatusEffectBlind other) => 
        new();

    public override void Apply(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs)
    {
        entMan.EnsureComponent<SurgeryLimbStatusBlindComponent>(target);
        entMan.System<BlindableSystem>().UpdateIsBlind(target);
    }
    public override void Remove(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs)
    {
        entMan.RemoveComponent<SurgeryLimbStatusBlindComponent>(target);
        entMan.System<BlindableSystem>().UpdateIsBlind(target);
    }

    public override void Update(IEntityManager entMan, EntityUid target, ILimbStatusEffect old, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs) => 
        Apply(entMan, target, organs);
}