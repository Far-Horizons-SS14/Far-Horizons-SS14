using System.Linq;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class DiseaseReagentData : ReagentData
{
    [DataField]
    public Dictionary<DiseaseData, StageData> ActiveDiseases = [];

    [DataField]
    public Dictionary<DiseaseData, float> Immunity = [];

    public override ReagentData Clone() => new DiseaseReagentData
    {
        ActiveDiseases = new Dictionary<DiseaseData, StageData>(ActiveDiseases),
        Immunity = new Dictionary<DiseaseData, float>(Immunity)
    };

    public override bool Equals(ReagentData? other)
    {
        if (other is not DiseaseReagentData d)
            return false;
        if (ReferenceEquals(this, d))
            return true;
        if (!ActiveDiseases.Keys.SequenceEqual(d.ActiveDiseases.Keys))
            return false;
        if (!Immunity.Keys.SequenceEqual(d.Immunity.Keys))
            return false;
        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var key in ActiveDiseases.Keys)
            hash.Add(key);
        foreach (var key in Immunity.Keys)
            hash.Add(key);
        return hash.ToHashCode();
    }
}
