using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared._FarHorizons.Medical.Disease.Cures;

[Serializable, NetSerializable]
public sealed partial class CureReagent : CureStep
{
    [DataField]
    public FixedPoint2 Min = 5;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>
    /// Cures the disease after the infection has lasted a configured duration.
    /// </summary>
    public override bool OnCure(EntityUid uid, DiseaseData disease)
    {
        var _entityManager = IoCManager.Resolve<IEntityManager>();
        var _solution = _entityManager.System<SharedSolutionContainerSystem>();

        if (!_entityManager.TryGetComponent<BloodstreamComponent>(uid, out var bloodstream) || 
            !_solution.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var solution))
            return false;
        
        var quant = solution.GetTotalPrototypeQuantity(Reagent);

        return quant >= Min && quant <= Max;
    }

    public override IEnumerable<string> BuildDiagnoserLines(IPrototypeManager prototypes)
    {
        yield return Loc.GetString("diagnoser-cure-reagent-item", ("units", Min), ("reagent", Reagent.Id));
    }
}
