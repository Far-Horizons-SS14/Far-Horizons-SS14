using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Medical.Disease.Effects;

[Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class ISymptomCondition
{
    public abstract bool Check(Entity<DiseaseCarrierComponent> ent, DiseaseData disease, StageData stage);
}
