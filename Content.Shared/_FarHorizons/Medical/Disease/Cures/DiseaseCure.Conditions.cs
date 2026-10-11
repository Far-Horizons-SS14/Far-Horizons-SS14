using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Disease.Cures;

[Serializable, NetSerializable]
public sealed partial class CureConditions : CureStep
{
    /// <summary>
    /// List of conditions needed to meet to cure someone.
    /// </summary>
    [DataField]
    public List<CureStep> Conditions = new();
}

public sealed partial class CureConditions
{
    /// <summary>
    /// Cures the disease when meeting the conditions listed above.
    /// </summary>
    public override bool OnCure(EntityUid uid, DiseaseData disease)
        => Conditions.All(p => p.OnCure(uid, disease)); 

    public override IEnumerable<string> BuildDiagnoserLines(IPrototypeManager prototypes)
    {
        foreach (var condition in Conditions)
        {
            var lines = condition.BuildDiagnoserLines(prototypes);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    yield return line;
            }
        }
    }
}
