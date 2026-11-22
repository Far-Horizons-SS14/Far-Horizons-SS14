using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Content.Shared._FarHorizons.Medical.Disease.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Disease;

/// <summary>
/// Applies a disease to the target, taking into account protection from the spread path.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class CauseDiseaseEntityEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, CauseDisease>
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedDiseaseSystem _disease = default!;

    protected override void Effect(Entity<DiseaseCarrierComponent> entity, ref EntityEffectEvent<CauseDisease> args)
    {
        if(!_prototype.TryIndex(args.Effect.DiseaseId, out var proto))
            return;

        var disease = _disease.GenerateDisease(args.Effect.DiseaseId);
        if(disease == null) return;

        var stage = _disease.CreateStage(disease.Value);
        if(stage == null) return;
            
        if (args.Effect.ForceInfect)
        {
            _disease.Infect(entity.Owner, disease.Value, stage);
            return;
        }

       if(!_disease.CanBeInfected(entity.Owner, disease.Value))
        return;

        switch (disease.Value.SpreadPath)
        {
            case DiseaseSpreadPath.Contact:
                {
                    var probability = _disease.AdjustContactChanceForProtection(entity.Owner, disease.Value.ContactInfect, disease.Value);
                    _disease.TryInfectWithChance(entity.Owner, disease.Value, stage, probability);
                    break;
                }
            case DiseaseSpreadPath.Airborne:
                {
                    var probability = _disease.AdjustAirborneChanceForProtection(entity.Owner, disease.Value.AirborneInfect, disease.Value);
                    _disease.TryInfectWithChance(entity.Owner, disease.Value, stage, probability);
                    break;
                }
            default:
                {
                    _disease.Infect(entity.Owner, disease.Value, stage);
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
