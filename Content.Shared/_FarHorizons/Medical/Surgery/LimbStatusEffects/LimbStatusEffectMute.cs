using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared._FarHorizons.Medical.Trauma;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Surgery.LimbStatusEffects;

public sealed partial class LimbStatusEffectMute : LimbStatusEffectGlobal<LimbStatusEffectMute>
{
    protected override bool Eq(LimbStatusEffectMute other) => true;

    protected override LimbStatusEffectMute Stack(LimbStatusEffectMute other) => 
        new();

    public override void Apply(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs) => 
        entMan.EnsureComponent<SurgeryLimbStatusMuteComponent>(target);
    public override void Remove(IEntityManager entMan, EntityUid target, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs) => 
        entMan.RemoveComponent<SurgeryLimbStatusMuteComponent>(target);
    public override void Update(IEntityManager entMan, EntityUid target, ILimbStatusEffect old, Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> organs) => 
        Apply(entMan, target, organs);
}