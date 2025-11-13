using System.Linq;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    public const string SpeciesLoadoutDatabaseKey = "__species_loadout"; // Database will store species loadout as this "job"

    [DataField]
    public RoleLoadout? SpeciesLoadout = null;

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

    public HumanoidCharacterProfile WithSpeciesLoadout(RoleLoadout? speciesLoadout) => 
    new(this) { SpeciesLoadout = speciesLoadout, };

    public RoleLoadout? GetSpeciesLoadoutOrDefault(ICommonSession? session, IPrototypeManager protoManager)
    {
        var speciesProto = protoManager.Index(Species);
        if (speciesProto.Loadout == null)
        {
            SpeciesLoadout = null;
            return SpeciesLoadout;
        }

        if (SpeciesLoadout == null)
        {
            SpeciesLoadout = new RoleLoadout(speciesProto.Loadout.Value);
            SpeciesLoadout.SetDefault(this, session, protoManager, force: true);
        }

        SpeciesLoadout.SetDefault(this, session, protoManager);
        return SpeciesLoadout;
    }
}