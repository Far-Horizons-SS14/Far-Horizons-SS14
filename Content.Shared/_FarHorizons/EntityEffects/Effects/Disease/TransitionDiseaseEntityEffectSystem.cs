using Content.Shared.Medical.Disease.Prototypes;
using Content.Shared.Medical.Disease.Components;
using Content.Shared.Medical.Disease.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Disease;

/// <summary>
/// Applies a disease transition effect to entities.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class TransitionDiseaseEntityEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, TransitionDisease>
{
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;

    protected override void Effect(Entity<DiseaseCarrierComponent> entity, ref EntityEffectEvent<TransitionDisease> args)
    {
        entity.Comp.ActiveDiseases.Remove(args.Effect.FromDiseaseId);
        _disease.Infect(entity.Owner, args.Effect.ToDiseaseId, Math.Max(1, args.Effect.StartStage));
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
