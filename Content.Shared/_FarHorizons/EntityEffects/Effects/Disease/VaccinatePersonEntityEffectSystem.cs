using System.Linq;
using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Metabolism;

namespace Content.Shared.EntityEffects.Effects.Disease;

/// <summary>
/// Applies the disease reagent's data for immunity to the vaccinated player
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class VaccinatePersonEntityEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, VaccinatePerson>
{
    protected override void Effect(Entity<DiseaseCarrierComponent> entity, ref EntityEffectEvent<VaccinatePerson> args)
    {
        var _entityManager = IoCManager.Resolve<IEntityManager>();
        var _entitySysManager = IoCManager.Resolve<IEntitySystemManager>();
        var _metabolism = _entitySysManager.GetEntitySystem<MetabolizerSystem>();
        var _solutions = _entitySysManager.GetEntitySystem<SharedSolutionContainerSystem>();
        var _disease = _entitySysManager.GetEntitySystem<SharedDiseaseSystem>();

        if (!_entityManager.TryGetComponent(entity.Owner, out BloodstreamComponent? bloodstream))
            return;

        if (!_solutions.ResolveSolution(entity.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var solution))
            return;
        
        var vaccine = solution.Contents.FirstOrDefault(r => r.Reagent.Prototype == "Vaccine");
        if (vaccine == default) return;

        var diseaseData = vaccine.Reagent.Data?.OfType<DiseaseReagentData>().FirstOrDefault();
        if (diseaseData == null) return;

        foreach (var (disease, value) in diseaseData.Immunity)
            entity.Comp.Immunity[disease] = value;
        
        _disease.UpdateBloodData(entity);
        _entityManager.Dirty(entity.Owner, entity.Comp);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class VaccinatePerson : EntityEffectBase<VaccinatePerson>;