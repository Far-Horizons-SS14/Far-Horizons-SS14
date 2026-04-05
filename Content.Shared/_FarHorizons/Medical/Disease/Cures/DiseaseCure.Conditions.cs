using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityConditions;
using Content.Shared.Metabolism;
using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._FarHorizons.Medical.Disease.Systems;

namespace Content.Shared._FarHorizons.Medical.Disease.Cures;

[Serializable, NetSerializable]
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
    /// <summary>
    /// Cure step that succeeds once its configured carrier conditions pass.
    /// </summary>
    public override bool OnCure(EntityUid uid, DiseaseData disease)
    {
        var _entityManager = IoCManager.Resolve<IEntityManager>();
        var _entitySysManager = IoCManager.Resolve<IEntitySystemManager>();
        var _metabolism = _entitySysManager.GetEntitySystem<MetabolizerSystem>();
        var _solutions = _entitySysManager.GetEntitySystem<SharedSolutionContainerSystem>();
        
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
