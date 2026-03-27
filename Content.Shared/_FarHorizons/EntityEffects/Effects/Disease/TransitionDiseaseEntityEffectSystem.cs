using Content.Shared.Medical.Disease.Prototypes;
using Content.Shared.Medical.Disease.Components;
using Content.Shared.Medical.Disease.Systems;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.EntityEffects.Effects.Disease;

/// <summary>
/// Applies a disease transition effect to entities.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class TransitionDiseaseEntityEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, TransitionDisease>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;

    protected override void Effect(Entity<DiseaseCarrierComponent> entity, ref EntityEffectEvent<TransitionDisease> args)
    {
        var FromDiseaseId = args.Effect.FromDiseaseId;
        var transitionFrom = entity.Comp.ActiveDiseases.Keys.First(x =>
        {
            if (!_prototype.TryIndex(FromDiseaseId, out var proto))
                return false;
            
            return true;
        });
        
        var disease = _disease.CreateDisease(args.Effect.ToDiseaseId);
        var stage = _disease.CreateStage(args.Effect.ToDiseaseId);
        if(disease == null || stage == null)
            return;

        if(! _disease.CanBeInfected(entity.Owner, disease))
            return;

        entity.Comp.ActiveDiseases.Remove(transitionFrom);
        _disease.Infect(entity.Owner, disease, stage);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class TransitionDisease : EntityEffectBase<TransitionDisease>
{
    /// <summary>
    /// Disease to remove from the target before applying <see cref="ToDiseaseId"/>.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DiseasePrototype> FromDiseaseId;

    /// <summary>
    /// Disease to infect the target with.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DiseasePrototype> ToDiseaseId;

    /// <summary>
    /// Starting stage for the new disease.
    /// </summary>
    [DataField]
    public int StartStage = 1;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-transition-disease",
            ("chance", Probability),
            ("fromDisease", Loc.GetString(prototype.Index(FromDiseaseId).Name)),
            ("toDisease", Loc.GetString(prototype.Index(ToDiseaseId).Name)));
}
