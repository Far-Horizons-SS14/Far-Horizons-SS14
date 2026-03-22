using Content.Shared.Medical.Disease.Prototypes;
using Content.Shared.Medical.Disease.Cures;
using Content.Shared.Medical.Disease.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Disease;

/// <summary>
/// Applies a cure for the specified disease to the target.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class CureDiseaseEntityEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, CureDisease>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedDiseaseCureSystem _cure = default!;

    protected override void Effect(Entity<DiseaseCarrierComponent> entity, ref EntityEffectEvent<CureDisease> args)
    {
        if (!_prototype.TryIndex(args.Effect.DiseaseId, out var diseaseProto))
            return;

        if (args.Effect.LowerStage)
            _cure.ApplyCureDiseaseStage(entity, diseaseProto);
        else
            _cure.ApplyCureDisease(entity, diseaseProto);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class CureDisease : EntityEffectBase<CureDisease>
{
    /// <summary>
    /// Disease to cure on the target.
    /// </summary>
    [DataField("disease", required: true)]
    public ProtoId<DiseasePrototype> DiseaseId;

    /// <summary>
    /// If true, the cure lowers the disease stage by 1 instead of fully curing.
    /// </summary>
    [DataField]
    public bool LowerStage;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (LowerStage)
            return Loc.GetString("entity-effect-guidebook-cure-disease-lower-stage",
                ("chance", Probability),
                ("disease", Loc.GetString(prototype.Index(DiseaseId).Name)));
        else
            return Loc.GetString("entity-effect-guidebook-cure-disease",
                ("chance", Probability),
                ("disease", Loc.GetString(prototype.Index(DiseaseId).Name)));
    }
}
