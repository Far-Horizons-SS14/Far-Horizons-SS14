using Content.Shared._FarHorizons.Medical.Disease.Components;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Medical.Disease.Effects;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class SymptomStageCondition : ISymptomCondition
{
    [DataField] public int Min = 0;
    [DataField] public int Max = 4;
    
    public override bool Check(Entity<DiseaseCarrierComponent> ent, DiseaseData disease, StageData stage)
        => stage.Stage >= Min && stage.Stage <= Max;
}
