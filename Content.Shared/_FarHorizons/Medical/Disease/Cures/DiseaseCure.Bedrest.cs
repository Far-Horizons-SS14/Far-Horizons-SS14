using Content.Shared.Bed.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.Stunnable;
using Content.Shared._FarHorizons.Medical.Disease.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Content.Shared._FarHorizons.Medical.Disease.Systems;
using Robust.Shared.Timing;
using Content.Shared.Random.Helpers;

namespace Content.Shared._FarHorizons.Medical.Disease.Cures;

[Serializable, NetSerializable]
public sealed partial class CureBedrest : CureStep
{
    /// <summary>
    /// Base per-tick cure chance while buckled to a bed.
    /// </summary>
    [DataField]
    public float BedrestChance { get; private set; } = 0.05f;

    /// <summary>
    /// Multiplier to accumulation while the carrier is sleeping.
    /// </summary>
    [DataField]
    public float SleepMultiplier { get; private set; } = 3f;
}

public sealed partial class CureBedrest
{
    /// <summary>
    /// Rolls a cure chance each tick while buckled to a healing bed.
    /// Sleeping multiplies the cure chance.
    /// </summary>
    public override bool OnCure(EntityUid uid, DiseaseData disease)
    {
        var _entityManager = IoCManager.Resolve<IEntityManager>();
        var _timing = IoCManager.Resolve<IGameTiming>();

        var onBed = false;
        if (_entityManager.TryGetComponent(uid, out BuckleComponent? buckle) && buckle.BuckledTo is { } strappedTo)
            onBed = _entityManager.HasComponent<HealOnBuckleComponent>(strappedTo);

        var knockedDown = _entityManager.HasComponent<KnockedDownComponent>(uid);

        if (!onBed && !knockedDown)
            return false;

        var sleepingNow = _entityManager.HasComponent<SleepingComponent>(uid);
        var sleepMult = sleepingNow ? MathF.Max(1f, SleepMultiplier) : 1f;
        var chance = MathF.Max(0f, BedrestChance) * sleepMult;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, _entityManager.GetNetEntity(uid).Id);
        var rand = new System.Random(seed);

        return rand.Prob(chance);
    }

    public override IEnumerable<string> BuildDiagnoserLines(IPrototypeManager prototypes)
    {
        var baseChance = MathF.Max(0f, BedrestChance);
        var sleepMult = MathF.Max(1f, SleepMultiplier);
        var percent = MathF.Round(baseChance * 100f);
        yield return Loc.GetString("diagnoser-cure-bedrest", ("chance", percent), ("sleepMult", sleepMult));
    }
}
