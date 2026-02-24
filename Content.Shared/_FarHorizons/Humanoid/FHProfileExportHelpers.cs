using System.Linq;
using Content.Shared._CD.Records;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using YamlDotNet.RepresentationModel;

namespace Content.Shared._FarHorizons.Humanoid;

public sealed class FHProfileExportHelpers
{
    // Versioning is fucked and there's no good way to detect where export came from. I wanted to keep support for all kinds of ss14 versions, so I'm reading the file field by field yandev style.
    // Sorry if you feel offended by the following code
    public static HumanoidCharacterProfile BuildProfileFromExport(YamlMappingNode root, IPrototypeManager protoMan, ISharedFactionManager factions, ISerializationManager serialization)
    {
        if (!root.AllNodes.Any(node => node is YamlScalarNode { Value: "profile" }) ||
            root["profile"] is not YamlMappingNode profile) throw new InvalidOperationException("Profile not found");
        
        var speciesNode = profile.AllNodes.Any(node => node is YamlScalarNode { Value: "species" }) ? profile["species"] : null;
        var exportSpeciesId = (speciesNode as YamlScalarNode)?.Value ?? "";
        ProtoId<SpeciesPrototype> species =
            protoMan.TryIndex<SpeciesPrototype>(exportSpeciesId, out var parsedSpecies)
                ? parsedSpecies.ID
                : HumanoidCharacterProfile.DefaultSpecies;

        var humanoidProfile = HumanoidCharacterProfile.DefaultWithSpecies(species);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "customSpecieName" }) &&
            profile["customSpecieName"] is YamlScalarNode { Value: not null } customSpecieName)
            humanoidProfile = humanoidProfile.WithCustomSpecieName(customSpecieName.Value);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "name" }) &&
            profile["name"] is YamlScalarNode { Value: not null } name)
            humanoidProfile = humanoidProfile.WithName(name.Value);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "age" }) &&
            profile["age"] is YamlScalarNode { Value: not null } age &&
            int.TryParse(age.Value, out var parsedAge))
            humanoidProfile = humanoidProfile.WithAge(parsedAge);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "sex" }) &&
            profile["sex"] is YamlScalarNode { Value: not null } sex &&
            Enum.TryParse<Sex>(sex.Value, true, out var parsedSex))
            humanoidProfile = humanoidProfile.WithSex(parsedSex);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "gender" }) &&
            profile["gender"] is YamlScalarNode { Value: not null } gender &&
            Enum.TryParse<Gender>(gender.Value, true, out var parsedGender))
            humanoidProfile = humanoidProfile.WithGender(parsedGender);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "spawnPriority" }) &&
            profile["spawnPriority"] is YamlScalarNode { Value: not null } spawnPriority &&
            Enum.TryParse<SpawnPriorityPreference>(spawnPriority.Value, true, out var parsedSpawnPriority))
            humanoidProfile = humanoidProfile.WithSpawnPriorityPreference(parsedSpawnPriority);

        // In some versions flavorText is the only thing, on others it's a duplicate of physicalDescription.
        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "physicalDescription" }) &&
            profile["physicalDescription"] is YamlScalarNode { Value: not null } physicalDescription)
            humanoidProfile = humanoidProfile.WithPhysicalDesc(physicalDescription.Value);
        else if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "flavorText" }) &&
                 profile["flavorText"] is YamlScalarNode { Value: not null } flavorText)
            humanoidProfile = humanoidProfile.WithPhysicalDesc(flavorText.Value);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "personalityDescription" }) &&
            profile["personalityDescription"] is YamlScalarNode { Value: not null } personalityDescription)
            humanoidProfile = humanoidProfile.WithPersonalityDesc(personalityDescription.Value);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "personalNotes" }) &&
            profile["personalNotes"] is YamlScalarNode { Value: not null } personalNotes)
            humanoidProfile = humanoidProfile.WithPersonalNotes(personalNotes.Value);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "oOCNotes" }) &&
            profile["oOCNotes"] is YamlScalarNode { Value: not null } oocNotes)
            humanoidProfile = humanoidProfile.WithOOCNotes(oocNotes.Value);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "secrets" }) &&
            profile["secrets"] is YamlScalarNode { Value: not null } secrets)
            humanoidProfile = humanoidProfile.WithSecrets(secrets.Value);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "exploitableInfo" }) &&
            profile["exploitableInfo"] is YamlScalarNode { Value: not null } exploitableInfo)
            humanoidProfile = humanoidProfile.WithExploitable(exploitableInfo.Value);

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "cybernetics" }) &&
            profile["cybernetics"] is YamlSequenceNode cybernetics)
        {
            List<string> parsedCybernetics =
                cybernetics.Children.OfType<YamlScalarNode>().Select(node => node.Value ?? "").Where(p => p != "").ToList();
            humanoidProfile = humanoidProfile.WithCybernetics(parsedCybernetics);
        }

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "speciesLoadout" }))
        {
            var speciesLoadout =
                serialization.Read<RoleLoadout?>(profile["speciesLoadout"].ToDataNode());

            humanoidProfile = humanoidProfile.WithSpeciesLoadout(speciesLoadout);
        }

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "cosmaticDriftCharacterRecords" }))
        {
            var cdRecords =
                serialization.Read<PlayerProvidedCharacterRecords>(
                    profile["cosmaticDriftCharacterRecords"].ToDataNode(), notNullableOverride: true);

            humanoidProfile = humanoidProfile.WithCDCharacterRecords(cdRecords);
        }

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "_antagPreferences" }))
        {
            var antagPrefs =
                serialization.Read<HashSet<ProtoId<AntagPrototype>>>(
                    profile["_antagPreferences"].ToDataNode(), notNullableOverride: true);

            humanoidProfile = humanoidProfile.WithAntagPreferences(antagPrefs);
        }

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "_traitPreferences" }))
        {
            var traitPrefs =
                serialization.Read<HashSet<ProtoId<TraitPrototype>>>(
                    profile["_traitPreferences"].ToDataNode(), notNullableOverride: true);

            foreach (var trait in traitPrefs)
                humanoidProfile = humanoidProfile.WithTraitPreference(trait, protoMan);
        }

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "_loadouts" }))
        {
            var loadouts =
                serialization.Read<Dictionary<string, RoleLoadout>>(
                    profile["_loadouts"].ToDataNode(), notNullableOverride: true);
            
            foreach (var (_, loadout) in loadouts)
                humanoidProfile = humanoidProfile.WithLoadout(loadout);
        }

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "_factionJobPreferences" })) // This is FH export
        {
            var factionJobs =
                serialization.Read<HashSet<(ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job)>>(
                    profile["_factionJobPreferences"].ToDataNode(), notNullableOverride: true);
            humanoidProfile = humanoidProfile.WithJobPreferences(factionJobs);
        }
        else if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "_jobPreferences" })) // This is SL export
        {
            var jobs = serialization.Read<HashSet<ProtoId<JobPrototype>>>(profile["_jobPreferences"].ToDataNode(),
                notNullableOverride: true);

            HashSet<(ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job)> factionJobs = new();
            foreach (var jobAssignment in factions.ListFactionJobs())
            {
                if (!jobs.Contains(jobAssignment.Job))
                    continue;

                factionJobs.Add((jobAssignment.Faction, jobAssignment.Job));
            }
            humanoidProfile = humanoidProfile.WithJobPreferences(factionJobs);
        }
        else if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "_jobPriorities" })) // Wizden export
        {
            var jobs = serialization.Read<Dictionary<ProtoId<JobPrototype>, JobPriority>>(profile["_jobPriorities"].ToDataNode(),
                notNullableOverride: true);

            HashSet<(ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job)> factionJobs = new();
            foreach (var jobAssignment in factions.ListFactionJobs())
            {
                if (!jobs.Keys.Contains(jobAssignment.Job))
                    continue;

                factionJobs.Add((jobAssignment.Faction, jobAssignment.Job));
            }
            humanoidProfile = humanoidProfile.WithJobPreferences(factionJobs);
        }

        if (profile.AllNodes.Any(node => node is YamlScalarNode { Value: "appearance" }) && profile["appearance"] is YamlMappingNode appearance)
        {
            var exportAppearance =
                BuildAppearanceFromExport(species, humanoidProfile.Sex, appearance, protoMan, serialization);
            humanoidProfile = humanoidProfile.WithCharacterAppearance(exportAppearance);
        }

        return humanoidProfile;
    }

    public static HumanoidCharacterAppearance BuildAppearanceFromExport(ProtoId<SpeciesPrototype> species, Sex sex, YamlMappingNode appearance, IPrototypeManager protoMan, ISerializationManager serialization)
    {
        var markings = IoCManager.Resolve<MarkingManager>();
        var humanoidAppearance = HumanoidCharacterAppearance.DefaultWithSpecies(species, sex);

        if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "skinColor" }) &&
            appearance["skinColor"] is YamlScalarNode { Value: not null } skinColor &&
            Color.TryParse(skinColor.Value, out var parsedSkinColor))
            humanoidAppearance = humanoidAppearance.WithSkinColor(parsedSkinColor);
        
        if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "eyeColor" }) &&
            appearance["eyeColor"] is YamlScalarNode { Value: not null } eyeColor &&
            Color.TryParse(eyeColor.Value, out var parsedEyeColor))
            humanoidAppearance = humanoidAppearance.WithEyeColor(parsedEyeColor);
        
        if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "eyeGlowing" }) &&
            appearance["eyeGlowing"] is YamlScalarNode { Value: not null } eyeGlowing &&
            bool.TryParse(eyeGlowing.Value, out var parsedEyeGlowing))
            humanoidAppearance = humanoidAppearance.WithEyeGlowing(parsedEyeGlowing);
        
        if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "height" }) &&
            appearance["height"] is YamlScalarNode { Value: not null } height &&
            float.TryParse(height.Value, out var parsedHeight))
            humanoidAppearance = humanoidAppearance.WithHeight(parsedHeight);

        if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "width" }) &&
            appearance["width"] is YamlScalarNode { Value: not null } width &&
            float.TryParse(width.Value, out var parsedWidth))
            humanoidAppearance = humanoidAppearance.WithWidth(parsedWidth);

        List<Marking> portedMarkings = new(); // Markings

        if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "hair" }) &&
            appearance["hair"] is YamlScalarNode { Value: not null } hair)
        {
            List<Color> markingColors = new();
            var markingGlowing = false;

            if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "hairColor" }) &&
                appearance["hairColor"] is YamlScalarNode { Value: not null } hairColor &&
                Color.TryParse(hairColor.Value, out var parsedHairColor))
                markingColors.Add(parsedHairColor);
            
            if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "hairGlowing" }) &&
                appearance["hairGlowing"] is YamlScalarNode { Value: not null } hairGlowing &&
                bool.TryParse(hairGlowing.Value, out var parsedHairGlowing))
                markingGlowing = parsedHairGlowing;

            if (markingColors.Count == 0)
                markingColors.Add(Color.Black);

            portedMarkings.Add(new Marking(hair.Value, markingColors, markingGlowing));
        }

        if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "facialHair" }) &&
            appearance["facialHair"] is YamlScalarNode { Value: not null } facialHair)
        {
            List<Color> markingColors = new();
            var markingGlowing = false;

            if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "facialHairColor" }) &&
                appearance["facialHairColor"] is YamlScalarNode { Value: not null } facialHairColor &&
                Color.TryParse(facialHairColor.Value, out var parsedFacialHairColor))
                markingColors.Add(parsedFacialHairColor);
            
            if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "facialHairGlowing" }) &&
                appearance["facialHairGlowing"] is YamlScalarNode { Value: not null } facialHairGlowing &&
                bool.TryParse(facialHairGlowing.Value, out var parsedFacialHairGlowing))
                markingGlowing = parsedFacialHairGlowing;

            if (markingColors.Count == 0)
                markingColors.Add(Color.Black);

            portedMarkings.Add(new Marking(facialHair.Value, markingColors, markingGlowing));
        }

        if (appearance.AllNodes.Any(node => node is YamlScalarNode { Value: "markings" }))
        {
            if (appearance["markings"] is YamlSequenceNode) // Old list of markings
            {
                var parsedOldMarkings = serialization.Read<List<Marking>>(appearance["markings"].ToDataNode(), notNullableOverride: true);
                portedMarkings.AddRange(parsedOldMarkings);
            }
            else if (appearance["markings"] is YamlMappingNode) // New dict of a dict of a list of markings
            {
                var nubodyMarkings =
                    serialization
                        .Read<Dictionary<ProtoId<OrganCategoryPrototype>,
                            Dictionary<HumanoidVisualLayers, List<Marking>>>>(appearance["markings"].ToDataNode(),
                            notNullableOverride: true);

                foreach (var (_, organMarkings) in nubodyMarkings)
                foreach (var (_, layerMarkings) in organMarkings)
                foreach (var marking in layerMarkings)
                    portedMarkings.Add(marking); // To account for our additional layers, we still run convert on these
            }
        }

        if (portedMarkings.Count > 0)
            humanoidAppearance = humanoidAppearance.WithMarkings(markings.ConvertMarkings(portedMarkings, species));

        return humanoidAppearance;
    }
}