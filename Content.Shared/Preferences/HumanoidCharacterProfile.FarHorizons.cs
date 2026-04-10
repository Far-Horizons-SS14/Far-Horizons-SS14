using System.IO;
using System.Linq;
using Content.Shared._FarHorizons.Factions;
using Content.Shared._FarHorizons.Humanoid;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    public const string SpeciesLoadoutDatabaseKey = "__species_loadout"; // Database will store species loadout as this "job"

    [DataField]
    public RoleLoadout? SpeciesLoadout = null;
    
    [DataField]
    public Symspeech? Symspeech = null;
    
    [DataField]
    public Symspeech? SiliconSymspeech = null;

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
    
    public HumanoidCharacterProfile WithVoice(Symspeech symspeech) => 
        new(this) { Symspeech = symspeech };

    public HumanoidCharacterProfile WithSiliconVoice(Symspeech symspeech) => 
        new(this) { SiliconSymspeech = symspeech };

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

    private static bool SpeciesLoadoutEquals(RoleLoadout? A, RoleLoadout? B)
    {
        if (A == null != (B == null))
            return false;

        if (A != null && B != null)
        {   
            if (A.SelectedLoadouts.Count != B.SelectedLoadouts.Count)
                return false;
            
            foreach (var (k, v) in A.SelectedLoadouts)
                if (!B.SelectedLoadouts.TryGetValue(k, out var bValue) || !bValue.SequenceEqual(v))
                    return false;
        }

        return true;
    }

    public static HumanoidCharacterProfile FromStream(Stream stream, ICommonSession session, ISerializationManager? serialization = null, IConfigurationManager? configuration = null)
    {
        IoCManager.Resolve(ref serialization);
        IoCManager.Resolve(ref configuration);

        using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
        var yamlStream = new YamlStream();
        yamlStream.Load(reader);

        var root = yamlStream.Documents[0].RootNode;
        HumanoidCharacterProfile profile;

        if (root is not YamlMappingNode rootMap) throw new InvalidOperationException("Failed to parse file");

        if (rootMap.AllNodes.Any(node => node is YamlScalarNode { Value: "fhVersion" })) // Far Horizons file, just parse it
        {
            var export = serialization.Read<HumanoidProfileExportFH>(rootMap.ToDataNode(), notNullableOverride: true);
            profile = export.Profile;
        }
        else
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            var factions = IoCManager.Resolve<ISharedFactionManager>();

            profile = FHProfileExportHelpers.BuildProfileFromExport(rootMap, protoMan, factions, serialization);
        }

        var collection = IoCManager.Instance;
        profile.EnsureValid(session, collection!);
        return profile;
    }
}