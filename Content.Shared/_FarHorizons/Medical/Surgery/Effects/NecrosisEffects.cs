using System.Linq;
using Content.Shared._FarHorizons.Medical.LimbStatusEffect;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Random.Helpers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Medical.Surgery.Effects;

public sealed partial class SurgeryApplyRandomNecrosisStatus : ISurgeryStepEffect
{
    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (IoCManager.Resolve<INetManager>().IsClient || // No prediction :c
            !entMan.TryGetComponent<NecrosisSurgeryTargetComponent>(target, out var surgeryTarget) ||
            !entMan.TryGetComponent<BodyComponent>(target, out var body) ||
            body.Organs == null)
            return;

        var organsByCategory = new Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid>();
        foreach (var uid in body.Organs.ContainedEntities)
            if (entMan.GetComponentOrNull<OrganComponent>(uid)?.Category is { } category)
                organsByCategory[category] = uid;

        var excludedStatuses = new HashSet<ProtoId<LimbStatusEffectPrototype>>();
        foreach (var (status, organ) in surgeryTarget.StatusOrganDependency)
            if (!organsByCategory.ContainsKey(organ))
                excludedStatuses.Add(status);

        var existing = limbStatus.GetStatusPerLimb(target)
            .ToDictionary(p => p.Key, p => p.Value);

        var candidates = new List<(EntityUid limbEnt, ProtoId<LimbStatusEffectPrototype> status)>();
        foreach (var (limbId, allowed) in surgeryTarget.NecrosisTarget)
        {
            if (!organsByCategory.TryGetValue(limbId, out var limbEnt))
                continue;

            existing.TryGetValue(limbId, out var current);

            foreach (var status in allowed)
            {
                if (excludedStatuses.Contains(status) || current?.Contains(status) == true)
                    continue;

                candidates.Add((limbEnt, status));
            }
        }

        if (candidates.Count == 0)
            return;

        var timing = IoCManager.Resolve<IGameTiming>();
        var random = SharedRandomExtensions.PredictedRandom(timing, entMan.GetNetEntity(target));

        var (pickedLimb, pickedStatus) = candidates[random.Next(candidates.Count)];
        limbStatus.SetStatusEffect(pickedLimb, pickedStatus);
    }
}

public sealed partial class SurgeryReduceNecrosisTime : ISurgeryStepEffect
{
    public void Run(IEntityManager entMan, LimbStatusEffectSystem limbStatus, EntityUid target, EntityUid? limb, EntityUid? tool)
    {
        if (!entMan.TryGetComponent<NecrosisSurgeryTargetComponent>(target, out var surgeryTarget))
            return;
        
        entMan.System<SharedRottingSystem>().ReduceAccumulator(target, surgeryTarget.NecrosisStatusCost);
    }
}