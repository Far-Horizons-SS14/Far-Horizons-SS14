using System.Linq;
using Content.Shared.FarHorizons.Factions;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    private static HashSet<(ProtoId<FactionPrototype>, ProtoId<JobPrototype>)> ValidateFactionJobPreferences(HashSet<(ProtoId<FactionPrototype>, ProtoId<JobPrototype>)> factionJobPreferences)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        return [.. factionJobPreferences.Where(
            factionJob =>
                      prototypeManager.TryIndex(factionJob.Item1, out var faction) &&
                      prototypeManager.TryIndex(factionJob.Item2, out var job) &&
                      faction.Playable &&
                      job is { SetPreference: true, Hidden: false }
            )];
    }
}