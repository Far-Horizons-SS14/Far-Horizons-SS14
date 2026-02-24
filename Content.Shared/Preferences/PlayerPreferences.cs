using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared._FarHorizons.Factions;

namespace Content.Shared.Preferences
{
    /// <summary>
    ///     Contains all player characters and the player's selected job priorities
    ///     Serialized both over the network and to disk.
    /// </summary>
    [Serializable]
    [NetSerializable]
    public sealed class PlayerPreferences
    {
        private Dictionary<int, HumanoidCharacterProfile> _characters;

        // Far Horizons
        public PlayerPreferences(IEnumerable<KeyValuePair<int, HumanoidCharacterProfile>> characters, Color adminOOCColor, List<ProtoId<ConstructionPrototype>> constructionFavorites,  Dictionary<(ProtoId<FactionPrototype>, ProtoId<JobPrototype>), JobPriority> jobPriorities)
        {
            _characters = new Dictionary<int, HumanoidCharacterProfile>(characters);
            AdminOOCColor = adminOOCColor;
            ConstructionFavorites = constructionFavorites;
            JobPriorities = SanitizeJobPriorities(jobPriorities);
        }

        // Far Horizons
        private static Dictionary<(ProtoId<FactionPrototype>, ProtoId<JobPrototype>), JobPriority> SanitizeJobPriorities(Dictionary<(ProtoId<FactionPrototype>, ProtoId<JobPrototype>), JobPriority> jobPriorities) => 
            jobPriorities.Where(kvp => kvp.Value != JobPriority.Never).ToDictionary();

        /// <summary>
        ///     All player characters.
        /// </summary>
        public IReadOnlyDictionary<int, HumanoidCharacterProfile> Characters => _characters;

        public HumanoidCharacterProfile GetProfile(int index)
        {
            return _characters[index];
        }

        // Far Horizons
        public Dictionary<(ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job), JobPriority> JobPriorities { get; set; }

        public Color AdminOOCColor { get; set; }

        /// <summary>
        ///    List of favorite items in the construction menu.
        /// </summary>
        public List<ProtoId<ConstructionPrototype>> ConstructionFavorites { get; set; } = [];

        public int IndexOfCharacter(HumanoidCharacterProfile profile)
        {
            return _characters.FirstOrNull(p => p.Value == profile)?.Key ?? -1;
        }

        public bool TryIndexOfCharacter(HumanoidCharacterProfile profile, out int index)
        {
            return (index = IndexOfCharacter(profile)) != -1;
        }

        /// <summary>
        /// Get job priorities, but filtered by the presence of enabled characters asking for that job
        /// </summary>
        /// Far Horizons
        public Dictionary<(ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job), JobPriority> JobPrioritiesFiltered()
        {
            var allCharacterJobs = new HashSet<(ProtoId<FactionPrototype>, ProtoId<JobPrototype>)>();
            foreach (var profile in Characters.Values)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;
                allCharacterJobs.UnionWith(humanoid.JobPreferences);
            }

            var filteredPlayerJobs = new Dictionary<(ProtoId<FactionPrototype>, ProtoId<JobPrototype>), JobPriority>();
            foreach (var ((faction, job), priority) in JobPriorities)
            {
                if (!allCharacterJobs.Contains((faction, job)))
                    continue;
                filteredPlayerJobs.Add((faction, job), priority);
            }

            return filteredPlayerJobs;
        }

        /// <summary>
        /// Given a job and faction, return a random enabled character asking for this job in this faction
        /// </summary>
        /// Far Horizons
        public HumanoidCharacterProfile? SelectProfileForJob(ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job)
        {
            List<HumanoidCharacterProfile> pool = [.. Characters.Values
                                                .Where(p =>
                                                        p is HumanoidCharacterProfile { Enabled: true } humanoid &&
                                                        humanoid.JobPreferences.Contains((faction, job)))
                                                .Select(p => (HumanoidCharacterProfile)p)];

        var random = IoCManager.Resolve<IRobustRandom>();
        return pool.Count == 0 ? null : random.Pick(pool);
    }

        /// <summary>
        /// Get all enabled profiles asking for a job
        /// </summary>
        /// Far Horizons
        public Dictionary<int, HumanoidCharacterProfile> GetAllEnabledProfilesForJob(ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job) => 
            GetAllProfilesForJobInternal(faction, job, onlyEnabled: true);

        /// <summary>
        /// Get all profiles asking for a job
        /// </summary>
        /// Far Horizons
        public Dictionary<int, HumanoidCharacterProfile> GetAllProfilesForJob(ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job) => 
            GetAllProfilesForJobInternal(faction, job, onlyEnabled: false);

        private Dictionary<int, HumanoidCharacterProfile> GetAllProfilesForJobInternal(ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job, bool onlyEnabled) => 
                Characters
                    .Where(kv =>
                        kv.Value is HumanoidCharacterProfile humanoid &&
                        (humanoid.Enabled || !onlyEnabled) &&
                        humanoid.JobPreferences.Contains((faction, job)))
                    .Select(kv => (kv.Key, (HumanoidCharacterProfile)kv.Value))
                    .ToDictionary();

        /// <summary>
        /// Get all enabled profiles asking for an antag
        /// </summary>
        public Dictionary<int, HumanoidCharacterProfile> GetAllEnabledProfilesForAntag(ProtoId<AntagPrototype> antag)
        {
            return GetAllProfilesForAntagInternal(antag, onlyEnabled: true);
        }

        /// <summary>
        /// Get all profiles asking for an antag
        /// </summary>
        public Dictionary<int, HumanoidCharacterProfile> GetAllProfilesForAntag(ProtoId<AntagPrototype> antag)
        {
            return GetAllProfilesForAntagInternal(antag, onlyEnabled: false);
        }

        private Dictionary<int, HumanoidCharacterProfile> GetAllProfilesForAntagInternal(ProtoId<AntagPrototype> antag, bool onlyEnabled)
        {
            var result = new Dictionary<int, HumanoidCharacterProfile>();
            foreach (var (slot, profile) in Characters)
            {
                if (profile is not HumanoidCharacterProfile humanoid)
                    continue;
                if (onlyEnabled && !humanoid.Enabled)
                    continue;
                if (humanoid.AntagPreferences.Contains(antag))
                    result.Add(slot, humanoid);
            }

            return result;
        }

        /// <summary>
        /// Get any random enabled profile
        /// </summary>
        public HumanoidCharacterProfile? GetRandomEnabledProfile()
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var pool = Characters.Values.Where(p => p is HumanoidCharacterProfile { Enabled: true }).ToList();
            return pool.Count == 0 ? null : random.Pick(pool) as HumanoidCharacterProfile;
        }

        /// <summary>
        /// Given an antag, return a random enabled character asking for this antag
        /// </summary>
        public HumanoidCharacterProfile? SelectProfileForAntag(ICollection<ProtoId<AntagPrototype>> antags)
        {
            var pool = new HashSet<HumanoidCharacterProfile>();
            foreach (var profile in Characters.Values)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;
                foreach (var antag in antags)
                {
                    if (humanoid.AntagPreferences.Contains(antag))
                        pool.Add(humanoid);
                }
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            return pool.Count == 0 ? null : random.Pick(pool);
        }

        /// <summary>
        /// Return true if the profile in the slot exists and is a HumanoidCharacterProfile
        /// </summary>
        public bool TryGetHumanoidInSlot(int slot, [NotNullWhen(true)] out HumanoidCharacterProfile? humanoid)
        {
            humanoid = null;
            if (!Characters.TryGetValue(slot, out var profile))
                return false;
            humanoid = profile as HumanoidCharacterProfile;
            return humanoid != null;
        }
    }
}
