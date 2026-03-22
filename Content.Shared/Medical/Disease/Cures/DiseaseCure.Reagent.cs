using Content.Shared.FixedPoint;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Medical.Disease.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Disease.Cures;

[DataDefinition]
public sealed partial class CureReagent : CureStep
{
    /// <summary>
    /// List of required reagents.
    /// </summary>
    [DataField(required: true)]
    public List<Requirement> Requirements { get; private set; } = [];

    [DataDefinition]
    public sealed partial class Requirement
    {
        /// <summary>
        /// The reagent that needs to be present.
        /// </summary>
        [DataField(required: true)]
        public ProtoId<ReagentPrototype> ReagentId { get; private set; } = default!;

        /// <summary>
        /// How much of the reagent must be present.
        /// </summary>
        [DataField]
        public FixedPoint2 Amount { get; private set; } = FixedPoint2.New(1);
    }
}

public sealed partial class CureReagent
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    /// <summary>
    /// Cures the disease if the bloodstream has the required reagents. Does not consume them.
    /// </summary>
    public override bool OnCure(EntityUid uid, DiseasePrototype disease)
    {
        if (!_entityManager.TryGetComponent(uid, out BloodstreamComponent? bloodstream))
            return false;

        if (!_solutions.ResolveSolution(uid, bloodstream.ChemicalSolutionName, ref bloodstream.ChemicalSolution, out var chemSolution))
            return false;

        if (Requirements.Count == 0)
            return false;

        foreach (var req in Requirements)
        {
            var have = chemSolution.GetTotalPrototypeQuantity(req.ReagentId);
            if (have < req.Amount)
                return false;
        }

        return true;
    }

    public override IEnumerable<string> BuildDiagnoserLines(IPrototypeManager prototypes)
    {
        var parts = new List<string>();
        foreach (var r in Requirements)
        {
            var name = r.ReagentId;
            if (prototypes.TryIndex(r.ReagentId, out var proto))
                name = proto.LocalizedName;

            parts.Add(Loc.GetString("diagnoser-cure-reagent-item", ("units", r.Amount.ToString()), ("reagent", name)));
        }

        var joined = string.Join(", ", parts);
        yield return Loc.GetString("diagnoser-cure-reagents-all", ("list", joined));
    }
}
