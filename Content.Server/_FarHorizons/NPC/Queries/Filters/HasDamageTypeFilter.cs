using Content.Server.NPC;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.NPC.Queries.Filters;

public sealed partial class HasDamageTypeFilter : ExternalFilter
{
    /// <summary>
    /// Damage group types to filter for.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<DamageGroupPrototype>> DamageGroups = new();

    /// <summary>
    /// Damage type types to filter for.
    /// </summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>> DamageTypes = new();

    public override List<EntityUid> GetEntities(NPCBlackboard blackboard, HashSet<EntityUid> entities, IEntityManager entMan)
    {
        var damageable = entMan.System<DamageableSystem>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        _entityList.Clear();

        var filterTypes = new HashSet<ProtoId<DamageTypePrototype>>(DamageTypes);
        foreach (var group in DamageGroups)
        {
            foreach (var type in protoMan.Index(group).DamageTypes)
                filterTypes.Add(type);
        }

        foreach (var ent in entities)
        {
            if (entMan.TryGetComponent<DamageableComponent>(ent, out var dmgComp) 
            && HasMatchingDamage(damageable.GetPositiveDamage((ent, dmgComp)), filterTypes))
                continue; 

            _entityList.Add(ent);
        }       

        return _entityList;
    }

    private bool HasMatchingDamage(DamageSpecifier damage, HashSet<ProtoId<DamageTypePrototype>> filter)
    {
        foreach (var (type, value) in damage.DamageDict)
        {
            if(filter.Contains(type) && value > FixedPoint2.Zero)
                return true;
        }
        return false;
    }
}
