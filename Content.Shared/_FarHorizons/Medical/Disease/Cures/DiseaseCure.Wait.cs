using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Content.Shared._FarHorizons.Medical.Disease.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Medical.Disease.Cures;

[Serializable, NetSerializable]
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
    /// <summary>
    /// Cures the disease after the infection has lasted a configured duration.
    /// </summary>
    public override bool OnCure(EntityUid uid, DiseaseData disease)
    {
        var _entityManager = IoCManager.Resolve<IEntityManager>();
        var _entitySysManager = IoCManager.Resolve<IEntitySystemManager>();
        var _timing = IoCManager.Resolve<IGameTiming>();
        var _cureSystem = _entitySysManager.GetEntitySystem<SharedDiseaseCureSystem>();

        if (RequiredTicks <= 0f)
            return false;

        var state = _cureSystem.GetState(uid, disease.Id, this);
        state.Ticker++;
        if (state.Ticker < RequiredTicks)
            return false;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, _entityManager.GetNetEntity(uid).Id);
        var rand = new System.Random(seed);

        if (rand.Prob(CureChance))
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
