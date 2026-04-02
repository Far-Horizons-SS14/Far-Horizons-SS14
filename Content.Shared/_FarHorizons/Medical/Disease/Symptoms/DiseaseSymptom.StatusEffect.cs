using Content.Shared.EntityEffects;
using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Content.Shared._FarHorizons.Medical.Disease.Systems;

namespace Content.Shared._FarHorizons.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomStatusEffect : SymptomBehavior
{
    /// <summary>
    /// List of effects to execute on symptom trigger. Supports any <see cref="EntityEffect"/>.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects { get; private set; } = [];
}

public sealed partial class SymptomStatusEffect
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    /// <summary>
    /// Executes the status effects.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseaseData disease)
    {
        if (Effects.Length == 0)
            return;

        _effects.ApplyEffects(uid, Effects);
    }
}
