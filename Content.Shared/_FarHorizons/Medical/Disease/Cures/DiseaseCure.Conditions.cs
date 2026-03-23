using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityConditions;
using Content.Shared.Metabolism;
using Content.Shared.Medical.Disease.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Disease.Cures;

[DataDefinition]
public sealed partial class CureConditions : CureStep
{
    /// <summary>
    /// Conditions checked on the disease carrier.
    /// </summary>
    [DataField(required: true)]
    public EntityCondition[] Conditions { get; private set; } = [];
}

public sealed partial class CureConditions
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly MetabolizerSystem _metabolism = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    /// <summary>
    /// Cure step that succeeds once its configured carrier conditions pass.
    /// </summary>
    public override bool OnCure(EntityUid uid, DiseasePrototype disease)
    {
        if (!_entityManager.TryGetComponent(uid, out BloodstreamComponent? bloodstream))
            return false;

        if (!_solutions.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution))
            return false;

        return _metabolism.CanMetabolizeEffect(uid, uid, bloodstream.BloodSolution.Value, Conditions);
    }

    public override IEnumerable<string> BuildDiagnoserLines(IPrototypeManager prototypes)
    {
        foreach (var condition in Conditions)
        {
            var line = condition.EntityConditionGuidebookText(prototypes);
            if (!string.IsNullOrWhiteSpace(line))
                yield return line;
        }
    }
}
