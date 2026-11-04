using Content.Shared.Random;
using Content.Shared.Salvage.Expeditions;
using Robust.Shared.Prototypes;

namespace Content.Server.Salvage;

public sealed partial class SpawnSalvageMissionJob
{
    private static void RemoveMaxed(
        List<IBudgetEntry> entries,
        Dictionary<EntProtoId, int> counts,
        ref float probSum)
    {
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i] is not SalvageMobEntry mob || mob.Max == null)
                continue;

            if (counts.GetValueOrDefault(mob.Proto) < mob.Max)
                continue;

            probSum -= mob.Prob;
            entries.RemoveAt(i);
        }
    }
}