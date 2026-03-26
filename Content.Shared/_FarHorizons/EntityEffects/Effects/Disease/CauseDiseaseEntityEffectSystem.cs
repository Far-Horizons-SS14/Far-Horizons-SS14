using Content.Shared.Medical.Disease.Prototypes;
using Content.Shared.Medical.Disease.Systems;
using Content.Shared.Medical.Disease.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Disease;

/// <summary>
/// Applies a disease to the target, taking into account protection from the spread path.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class CauseDiseaseEntityEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, CauseDisease>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;

    protected override void Effect(Entity<DiseaseCarrierComponent> entity, ref EntityEffectEvent<CauseDisease> args)
    {
        if(!_prototype.TryIndex(args.Effect.DiseaseId, out var proto))
            return;

        var disease = _disease.CreateDisease(args.Effect.DiseaseId);
        var stage = _disease.CreateStage(args.Effect.DiseaseId, 1);
        if(disease == null || stage == null)
            return;
            
        if (args.Effect.ForceInfect)
        {
            _disease.Infect(entity.Owner, disease, stage);
            return;
        }

       if(!_disease.CanBeInfected(entity.Owner, disease))
        return;

        switch (proto.SpreadPath)
        {
            case DiseaseSpreadPath.Contact:
                {
                    var probability = _disease.AdjustContactChanceForProtection(entity.Owner, proto.ContactInfect, proto);
                    _disease.TryInfectWithChance(entity.Owner, disease, stage, probability);
                    break;
                }
            case DiseaseSpreadPath.Airborne:
                {
                    var probability = _disease.AdjustAirborneChanceForProtection(entity.Owner, proto.AirborneInfect, proto);
                    _disease.TryInfectWithChance(entity.Owner, disease, stage, probability);
                    break;
                }
            default:
                {
                    _disease.Infect(entity.Owner, disease, stage);
                    break;
                }
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class CauseDisease : EntityEffectBase<CauseDisease>
{
    /// <summary>
    /// Disease to infect the target with.
    /// </summary>
    [DataField("disease", required: true)]
    public ProtoId<DiseasePrototype> DiseaseId;

    /// <summary>
    /// If true, causes the disease to infect the target, ignoring protection from the spread path.
    /// </summary>
    [DataField]
    public bool ForceInfect;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-cause-disease",
            ("chance", Probability),
            ("disease", Loc.GetString(prototype.Index(DiseaseId).Name)));
}
