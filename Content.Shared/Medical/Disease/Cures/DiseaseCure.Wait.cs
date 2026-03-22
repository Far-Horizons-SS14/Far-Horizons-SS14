using Content.Shared.Medical.Disease.Prototypes;
using Content.Shared.Medical.Disease.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Disease.Cures;

[DataDefinition]
public sealed partial class CureWait : CureStep
{
    /// <summary>
    /// Ticks since infection required before curing can occur.
    /// </summary>
    [DataField]
    public int RequiredTicks { get; private set; } = 90;
}

public sealed partial class CureWait
{
    [Dependency] private readonly SharedDiseaseCureSystem _cureSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Cures the disease after the infection has lasted a configured duration.
    /// </summary>
    public override bool OnCure(EntityUid uid, DiseasePrototype disease)
    {
        if (RequiredTicks <= 0f)
            return false;

        var state = _cureSystem.GetState(uid, disease.ID, this);
        state.Ticker++;
        if (state.Ticker < RequiredTicks)
            return false;

        if (_random.Prob(CureChance))
        {
            state.Ticker = 0;
            return true;
        }

        state.Ticker = 0;
        return false;
    }

    public override IEnumerable<string> BuildDiagnoserLines(IPrototypeManager prototypes)
    {
        var defaultTickSeconds = new DiseaseCarrierComponent().TickDelay.TotalSeconds;
        var seconds = RequiredTicks * defaultTickSeconds;
        yield return Loc.GetString("diagnoser-cure-time", ("time", seconds));
    }
}
