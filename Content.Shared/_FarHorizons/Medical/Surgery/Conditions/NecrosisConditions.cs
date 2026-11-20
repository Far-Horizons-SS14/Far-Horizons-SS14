using System.Linq;
using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Atmos.Rotting;
using Robust.Shared.Network;

namespace Content.Shared._FarHorizons.Medical.Surgery.Conditions;

public sealed partial class SurgeryHasUnadressedNecrosis : ISurgeryStepCondition
{
    [DataField] public bool Inverse { get; set; }
    [DataField] public int CapTotalStatuses = 5;

    public bool Validate(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool) => 
        (GetRemainingRotTime(entMan, limbStatus, target, CapTotalStatuses) > TimeSpan.Zero) ^ Inverse;

    public static TimeSpan GetRemainingRotTime(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, int capTotal)
    {
        if (!entMan.TryGetComponent<RottingComponent>(target, out var rotting) ||
            !entMan.TryGetComponent<NecrosisSurgeryTargetComponent>(target, out var surgeryTarget))
            return TimeSpan.Zero;
        
        if (IoCManager.Resolve<INetManager>().IsClient)
            return TimeSpan.MaxValue; // No prediction, so on client we assume there's rot

        var totalMatches = 0;
        foreach (var (limbId, statuses) in limbStatus.GetStatusPerLimb(target))
        {
            if (!surgeryTarget.NecrosisTarget.TryGetValue(limbId, out var allowed))
                continue;

            totalMatches += statuses.Count(allowed.Contains);
        }

        if (totalMatches >= capTotal)
            return TimeSpan.Zero;

        var unusedTime = surgeryTarget.NecrosisStatusCost * totalMatches;

        return rotting.TotalRotTime - unusedTime;
    }
}
