using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Medical.Disease.Effects;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class SymptomDamageCondition : ISymptomCondition
{
    [DataField] public List<ProtoId<DamageTypePrototype>> types = new();
    [DataField] public FixedPoint2 Min = FixedPoint2.Zero;
    [DataField] public FixedPoint2 Max = FixedPoint2.MaxValue;

    public override bool Check(Entity<DiseaseCarrierComponent> ent, DiseaseData disease, StageData stage)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var damage = entMan.System<DamageableSystem>();

        if(!entMan.TryGetComponent<DamageableComponent>(ent, out var damageable))
            return false;

        
        var currentDamage = damage.GetPositiveDamage((ent.Owner, damageable));

        if(types.Count > 0)
        {
            foreach(var type in types)
            {
                var typeDamage = currentDamage.DamageDict.GetValueOrDefault(type, FixedPoint2.Zero);
                if(typeDamage < Min || typeDamage > Max)
                    return false;
            }

            return true;
        }

        var total = currentDamage.GetTotal();
        return total >= Min && total <= Max;
    }
}