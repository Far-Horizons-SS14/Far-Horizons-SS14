using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Body;
using Content.Shared.CCVar;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Preferences.Loadouts;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;
using Content.Shared._FarHorizons.Factions;
using Content.Server._FarHorizons.Factions;
using Content.Shared._CD.Records;
using Content.Shared._Starlight.Traits;
using Content.Shared.Starlight.TextToSpeech; // Far Horizons edit

namespace Content.Server.Preferences.Managers
{
    /// <summary>
    /// Sends <see cref="MsgPreferencesAndSettings"/> before the client joins the lobby.
    /// Receives <see cref="MsgSetCharacterEnable"/>, <see cref="MsgUpdateCharacter"/>,
    /// <see cref="MsgDeleteCharacter"/>, and <see cref="MsgUpdateJobPriorities"/> at any time.
    /// </summary>
    public sealed class ServerPreferencesManager : IServerPreferencesManager, IPostInjectInit
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IDependencyCollection _dependencies = default!;
        [Dependency] private readonly ILogManager _log = default!;
        [Dependency] private readonly UserDbDataManager _userDb = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IServerFactionManager _factions = default!; // Far Horizons
        [Dependency] private readonly MarkingManager _marking = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;

        // Cache player prefs on the server so we don't need as much async hell related to them.
        private readonly Dictionary<NetUserId, PlayerPrefData> _cachedPlayerPrefs =
            new();

        private ISawmill _sawmill = default!;

        private int MaxCharacterSlots => _cfg.GetCVar(CCVars.GameMaxCharacterSlots);

        public void Init()
        {
            _netManager.RegisterNetMessage<MsgPreferencesAndSettings>();
            _netManager.RegisterNetMessage<MsgUpdateCharacter>(HandleUpdateCharacterMessage);
            _netManager.RegisterNetMessage<MsgDeleteCharacter>(HandleDeleteCharacterMessage);
            _netManager.RegisterNetMessage<MsgSetCharacterEnable>(HandleSetCharacterEnableMessage);
            _netManager.RegisterNetMessage<MsgUpdateJobPriorities>(HandleUpdateJobPrioritiesMessage);
            _netManager.RegisterNetMessage<MsgUpdateConstructionFavorites>(HandleUpdateConstructionFavoritesMessage);
            _sawmill = _log.GetSawmill("prefs");
        }

        private static TValue? TryDeserialize<TValue>(JsonDocument document) where TValue : class
        {
            try
            {
                return document.Deserialize<TValue>();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        internal PlayerPreferences ConvertPreferences(Preference prefs)
        {
            var maxSlot = prefs.Profiles.Max(p => p.Slot) + 1;
            var profiles = new Dictionary<int, HumanoidCharacterProfile>(maxSlot);
            foreach (var profile in prefs.Profiles)
            {
                profiles[profile.Slot] = ConvertProfiles(profile);
            }

            var constructionFavorites = new List<ProtoId<ConstructionPrototype>>(prefs.ConstructionFavorites.Count);
            foreach (var favorite in prefs.ConstructionFavorites)
                constructionFavorites.Add(new ProtoId<ConstructionPrototype>(favorite));

            var jobs = prefs.JobPriorities.ToDictionary(
                p => ((ProtoId<FactionPrototype>)p.FactionName, (ProtoId<JobPrototype>)p.JobName),
                p => (JobPriority)p.Priority);

            return new PlayerPreferences(profiles, Color.FromHex(prefs.AdminOOCColor), constructionFavorites, jobs);
        }

        internal HumanoidCharacterProfile ConvertProfiles(Profile profile)
        {

            var jobs = profile.Jobs
                .Select(j => (new ProtoId<FactionPrototype>(j.FactionName), new ProtoId<JobPrototype>(j.JobName)))
                .ToHashSet(); // Far Horizons
            var antags = profile.Antags.Select(a => new ProtoId<AntagPrototype>(a.AntagName));
            var traits = profile.Traits.Select(t => new ProtoId<TraitPrototype>(t.TraitName));

            var sex = Sex.Male;
            if (Enum.TryParse<Sex>(profile.Sex, true, out var sexVal))
                sex = sexVal;

            var spawnPriority = (SpawnPriorityPreference) profile.SpawnPriority;

            var gender = sex == Sex.Male ? Gender.Male : Gender.Female;
            if (Enum.TryParse<Gender>(profile.Gender, true, out var genderVal))
                gender = genderVal;


            var markings =
                new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

            var species = profile.Species;
            if (!_prototypeManager.HasIndex<SpeciesPrototype>(species))
                species = HumanoidCharacterProfile.DefaultSpecies;

            if (profile.OrganMarkings?.RootElement is { } element)
            {
                var data = element.ToDataNode();
                markings = _serialization
                    .Read<Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>>(
                        data,
                        notNullableOverride: true);
            }
            else if (profile.Markings is { } profileMarkings && TryDeserialize<List<string>>(profileMarkings) is { } markingsRaw)
            {
                List<Marking> markingsList = new();

                foreach (var marking in markingsRaw)
                {
                    var parsed = Marking.ParseFromDbString(marking);

                    if (parsed is null) continue;

                    markingsList.Add(parsed.Value);
                }

                if (Marking.ParseFromDbString($"{profile.FacialHairName}@{profile.FacialHairColor}@{profile.FacialHairGlowing}") is { } facialMarking)
                    markingsList.Add(facialMarking);

                if (Marking.ParseFromDbString($"{profile.HairName}@{profile.HairColor}@{profile.HairGlowing}") is { } hairMarking)
                    markingsList.Add(hairMarking);

                markings = _marking.ConvertMarkings(markingsList, species);
            }

            var loadouts = new Dictionary<string, RoleLoadout>();

            foreach (var role in profile.Loadouts)
            {
                var loadout = new RoleLoadout(role.RoleName)
                {
                    EntityName = role.EntityName,
                };

                foreach (var group in role.Groups)
                {
                    var groupLoadouts = loadout.SelectedLoadouts.GetOrNew(group.GroupName);
                    foreach (var profLoadout in group.Loadouts)
                    {
                        groupLoadouts.Add(new Loadout()
                        {
                            Prototype = profLoadout.LoadoutName,
                        });
                    }
                }

                loadouts[role.RoleName] = loadout;
            }

            //start starlight
            var physicalDesc = string.Empty;
            var personalityDesc = string.Empty;
            var personalNotes = string.Empty;
            var oocNotes = string.Empty;
            var characterSecrets = string.Empty;
            var exploitableInfo = string.Empty;

            if (profile.CharacterInfo != null)
            {
                physicalDesc = profile.CharacterInfo.PhysicalDesc;
                if (string.IsNullOrEmpty(physicalDesc))
                    physicalDesc = profile.FlavorText;

                personalityDesc = profile.CharacterInfo.PersonalityDesc;
                personalNotes = profile.CharacterInfo.PersonalNotes;
                oocNotes = profile.CharacterInfo.OOCNotes;
                characterSecrets = profile.CharacterInfo.CharacterSecrets;
                exploitableInfo = profile.CharacterInfo.ExploitableInfo;
            }
            else if (string.IsNullOrEmpty(physicalDesc))
                physicalDesc = profile.FlavorText;

            //end starlight

            // Far Horizons start
            RoleLoadout? speciesLoadout = null;
            if (loadouts.Remove(HumanoidCharacterProfile.SpeciesLoadoutDatabaseKey, out var value))
                speciesLoadout = value;

            Symspeech? symspeech;
            Symspeech? siliconSymspeech;
            
            if (profile.FarHorizonsProfile?.Symspeech is { } profileSymspeech
                && _prototypeManager.HasIndex<VoicePrototype>(profileSymspeech.Voice))
            {
                symspeech = new Symspeech(
                    profileSymspeech.Voice,
                    profileSymspeech.Pitch,
                    profileSymspeech.Speed,
                    profileSymspeech.Pause,
                    profileSymspeech.Polyphony,
                    profileSymspeech.Volume
                );
            }
            else
                symspeech = null;

            if (profile.FarHorizonsProfile?.SiliconSymspeech is { } profileSiliconSymspeech
                && _prototypeManager.HasIndex<VoicePrototype>(profileSiliconSymspeech.Voice))
            {
                siliconSymspeech = new Symspeech(
                    profileSiliconSymspeech.Voice,
                    profileSiliconSymspeech.Pitch,
                    profileSiliconSymspeech.Speed,
                    profileSiliconSymspeech.Pause,
                    profileSiliconSymspeech.Polyphony,
                    profileSiliconSymspeech.Volume
                );
            }
            else
                siliconSymspeech = null;

            PlayerProvidedCharacterRecords? cdProfile = null;
            if (profile.CDProfile is { CharacterRecords: not null })
            {
                cdProfile = profile.CDProfile!.CharacterRecords.Deserialize<PlayerProvidedCharacterRecords>();

                var medicalEntries = profile.CDProfile!.CharacterRecordEntries
                    .Where(p => p.Type == CDModel.DbRecordEntryType.Medical).Select(p =>
                        new PlayerProvidedCharacterRecords.RecordEntry(p.Title, p.Involved, p.Description)).ToList();
                if (medicalEntries.Count > 0)
                    cdProfile = cdProfile?.WithMedicalEntries(medicalEntries);

                var secEntries = profile.CDProfile!.CharacterRecordEntries
                    .Where(p => p.Type == CDModel.DbRecordEntryType.Security).Select(p =>
                        new PlayerProvidedCharacterRecords.RecordEntry(p.Title, p.Involved, p.Description)).ToList();
                if (secEntries.Count > 0)
                    cdProfile = cdProfile?.WithSecurityEntries(secEntries);

                var employmentEntries = profile.CDProfile!.CharacterRecordEntries
                    .Where(p => p.Type == CDModel.DbRecordEntryType.Employment).Select(p =>
                        new PlayerProvidedCharacterRecords.RecordEntry(p.Title, p.Involved, p.Description)).ToList();
                if (employmentEntries.Count > 0)
                    cdProfile = cdProfile?.WithEmploymentEntries(employmentEntries);

                var adminEntries = profile.CDProfile!.CharacterRecordEntries
                    .Where(p => p.Type == CDModel.DbRecordEntryType.Admin).Select(p =>
                        new PlayerProvidedCharacterRecords.RecordEntry(p.Title, p.Involved, p.Description)).ToList();
                if (adminEntries.Count > 0)
                    cdProfile = cdProfile?.WithAdminEntries(adminEntries);
            }
            // Far Horizons end
            
            return new HumanoidCharacterProfile(
                profile.CharacterName,
                symspeech, // Far Horizons
                siliconSymspeech, // Far Horizons
                physicalDesc, // Starlight
                personalityDesc, // Starlight
                personalNotes, // Starlight
                oocNotes, // Starlight
                characterSecrets, // Starlight
                exploitableInfo, // Starlight
                species,
                profile.StarLightProfile?.CustomSpecieName ?? "", // Starlight
                profile.Age,
                sex,
                gender,
                new HumanoidCharacterAppearance
                (
                    Color.FromHex(profile.EyeColor),
                    profile.EyeGlowing, //starlight
                    Color.FromHex(profile.SkinColor),
                    markings,
                    profile.StarLightProfile?.Width ?? 1f, //starlight
                    profile.StarLightProfile?.Height ?? 1f //starlight
                ),
                spawnPriority,
                jobs,
                antags.ToHashSet(),
                traits.ToHashSet(),
                loadouts,
                profile.StarLightProfile?.CyberneticIds ?? [], // Starlight
                profile.Enabled,
                speciesLoadout, // Far Horizons
                cdProfile // Far Horizons
            );
        }

        // Far Horizons removed
        // private async void HandleSelectCharacterMessage(MsgSelectCharacter message)
        // {
        //     var index = message.SelectedCharacterIndex;
        //     var userId = message.MsgChannel.UserId;

        //     if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
        //     {
        //         _sawmill.Warning($"User {userId} tried to modify preferences before they loaded.");
        //         return;
        //     }

        //     if (index < 0 || index >= MaxCharacterSlots)
        //     {
        //         return;
        //     }

        //     var curPrefs = prefsData.Prefs!;

        //     if (!curPrefs.Characters.ContainsKey(index))
        //     {
        //         // Non-existent slot.
        //         return;
        //     }

        //     prefsData.Prefs = new PlayerPreferences(curPrefs.Characters, index, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites);

        //     if (ShouldStorePrefs(message.MsgChannel.AuthType))
        //     {
        //         await _db.SaveSelectedCharacterIndexAsync(message.MsgChannel.UserId, message.SelectedCharacterIndex);
        //     }
        // }

        private async void HandleUpdateCharacterMessage(MsgUpdateCharacter message)
        {
            var userId = message.MsgChannel.UserId;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (message.Profile == null)
                _sawmill.Error($"User {userId} sent a {nameof(MsgUpdateCharacter)} with a null profile in slot {message.Slot}.");
            else
                await SetProfile(userId, message.Slot, message.Profile);
        }

        public async Task SetProfile(NetUserId userId, int slot, HumanoidCharacterProfile profile)
        {
            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Error($"Tried to modify user {userId} preferences before they loaded.");
                return;
            }

            if (slot < 0 || slot >= MaxCharacterSlots)
                return;

            var curPrefs = prefsData.Prefs!;
            var session = _playerManager.GetSessionById(userId);

            profile.EnsureValid(session, _dependencies);

            var profiles = new Dictionary<int, HumanoidCharacterProfile>(curPrefs.Characters)
            {
                [slot] = profile
            };

            prefsData.Prefs = new PlayerPreferences(profiles, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites, curPrefs.JobPriorities);

            if (ShouldStorePrefs(session.Channel.AuthType))
            {
                try
                {
                    await _db.SaveCharacterSlotAsync(userId, profile, slot);
                }
                catch (Exception e)
                {
                    _sawmill.Error(
                        $"Error saving character {slot} for user {userId} " +
                        $"(symspeech voice='{profile.Symspeech?.Voice.Id ?? "<null>"}', " +
                        $"silicon voice='{profile.SiliconSymspeech?.Voice.Id ?? "<null>"}'): {e}");
                }
            }
        }

        public async Task SetConstructionFavorites(NetUserId userId, List<ProtoId<ConstructionPrototype>> favorites)
        {
            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Error($"Tried to modify user {userId} preferences before they loaded.");
                return;
            }

            var curPrefs = prefsData.Prefs!;
            prefsData.Prefs = new PlayerPreferences(curPrefs.Characters, curPrefs.AdminOOCColor, favorites, curPrefs.JobPriorities);

            var session = _playerManager.GetSessionById(userId);
            if (ShouldStorePrefs(session.Channel.AuthType))
                await _db.SaveConstructionFavoritesAsync(userId, favorites);
        }

        /// <summary>
        /// Update the job priorities dictionary for a given player
        /// </summary>
        /// Far Horizons
        public async Task SetJobPriorities(NetUserId userId, Dictionary<(ProtoId<FactionPrototype>, ProtoId<JobPrototype>), JobPriority> jobPriorities)
        {
            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Warning("prefs", $"User {userId} tried to modify preferences before they loaded.");
                return;
            }

            var curPrefs = prefsData.Prefs!;
            var session = _playerManager.GetSessionById(userId);

            prefsData.Prefs = new PlayerPreferences(curPrefs.Characters, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites, jobPriorities);

            if (ShouldStorePrefs(session.Channel.AuthType))
                await _db.SaveJobPrioritiesAsync(userId, jobPriorities);
        }

        private async void HandleDeleteCharacterMessage(MsgDeleteCharacter message)
        {
            var slot = message.Slot;
            var userId = message.MsgChannel.UserId;

            await DeleteProfile(userId, slot);
        }

        /// <summary>
        /// Delete a character profile for the given player in the given slot
        /// </summary>
        public async Task DeleteProfile(NetUserId userId, int slot)
        {
            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Warning("prefs", $"User {userId} tried to modify preferences before they loaded.");
                return;
            }

            if (slot < 0 || slot >= MaxCharacterSlots)
            {
                return;
            }

            var curPrefs = prefsData.Prefs!;
            var session = _playerManager.GetSessionById(userId);

            var arr = new Dictionary<int, HumanoidCharacterProfile>(curPrefs.Characters);
            arr.Remove(slot);

            prefsData.Prefs = new PlayerPreferences(arr, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites, curPrefs.JobPriorities);

            if (ShouldStorePrefs(session.AuthType))
            {
                await _db.SaveCharacterSlotAsync(userId, null, slot);
            }
        }

        /// <summary>
        /// Handle the net message from a client to enable or disable a character in a given slot
        /// </summary>
        private async void HandleSetCharacterEnableMessage(MsgSetCharacterEnable message)
        {
            var slot = message.CharacterIndex;
            var val = message.EnabledValue;

            var userId = message.MsgChannel.UserId;

            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Warning("prefs", $"User {userId} tried to modify preferences before they loaded.");
                return;
            }

            var curPrefs = prefsData.Prefs!;
            var session = _playerManager.GetSessionById(userId);

            if (!curPrefs.Characters.TryGetValue(slot, out var characterProfile))
            {
                // Non-existent slot.
                return;
            }

            if (characterProfile is not HumanoidCharacterProfile profile)
                return;

            profile.Enabled = val;
            var profiles = new Dictionary<int, HumanoidCharacterProfile>(curPrefs.Characters)
            {
                [slot] = new HumanoidCharacterProfile(profile),
            };

            prefsData.Prefs = new PlayerPreferences(profiles, curPrefs.AdminOOCColor, curPrefs.ConstructionFavorites, curPrefs.JobPriorities);

            if (ShouldStorePrefs(session.Channel.AuthType))
                await _db.SaveCharacterSlotAsync(userId, profile, slot);
        }

        /// <summary>
        /// Handler for the message from a client to update a player's job priorities dictionary
        /// </summary>
        public async void HandleUpdateJobPrioritiesMessage(MsgUpdateJobPriorities message)
        {
            var userId = message.MsgChannel.UserId;

            await SetJobPriorities(userId, message.JobPriorities);
        }

        private async void HandleUpdateConstructionFavoritesMessage(MsgUpdateConstructionFavorites message)
        {
            var userId = message.MsgChannel.UserId;
            if (!_cachedPlayerPrefs.TryGetValue(userId, out var prefsData) || !prefsData.PrefsLoaded)
            {
                _sawmill.Warning($"User {userId} tried to modify preferences before they loaded.");
                return;
            }

            // Validate items in the message so that a modified client cannot freely store a gigabyte of arbitrary data.
            var validatedSet = new HashSet<ProtoId<ConstructionPrototype>>();
            foreach (var favorite in message.Favorites)
            {
                if (_prototypeManager.HasIndex(favorite))
                    validatedSet.Add(favorite);
            }

            var validatedList = message.Favorites;
            if (validatedSet.Count != message.Favorites.Count)
            {
                // A difference in counts indicates that unrecognized or duplicate IDs are present.
                _sawmill.Warning($"User {userId} sent invalid construction favorites.");
                validatedList = validatedSet.ToList();
            }

            var curPrefs = prefsData.Prefs!;
            prefsData.Prefs = new PlayerPreferences(curPrefs.Characters, curPrefs.AdminOOCColor, validatedList, curPrefs.JobPriorities);

            if (ShouldStorePrefs(message.MsgChannel.AuthType))
            {
                await _db.SaveConstructionFavoritesAsync(userId, validatedList);
            }
        }

        // Should only be called via UserDbDataManager.
        public async Task LoadData(ICommonSession session, CancellationToken cancel)
        {
            if (!ShouldStorePrefs(session.Channel.AuthType))
            {
                // Don't store data for guests.
                var prefsData = new PlayerPrefData
                {
                    PrefsLoaded = true,
                    Prefs = new PlayerPreferences(
                        new[] { new KeyValuePair<int, HumanoidCharacterProfile>(0, HumanoidCharacterProfile.Random()) },
                        Color.Transparent, [], 
                        new Dictionary<(ProtoId<FactionPrototype>, ProtoId<JobPrototype>), JobPriority>{{ _factions.GetDefaultWithJob(), JobPriority.High }}) // Far Horizons
                };

                _cachedPlayerPrefs[session.UserId] = prefsData;
            }
            else
            {
                var prefsData = new PlayerPrefData();
                var loadTask = LoadPrefs();
                _cachedPlayerPrefs[session.UserId] = prefsData;

                await loadTask;

                async Task LoadPrefs()
                {
                    var prefs = await GetOrCreatePreferencesAsync(session.UserId, cancel);
                    prefsData.Prefs = ConvertPreferences(prefs);
                }
            }
        }

        public void FinishLoad(ICommonSession session)
        {
            // This is a separate step from the actual database load.
            // Sanitizing preferences requires play time info due to loadouts.
            // And play time info is loaded concurrently from the DB with preferences.
            var prefsData = _cachedPlayerPrefs[session.UserId];
            DebugTools.Assert(prefsData.Prefs != null);
            prefsData.Prefs = SanitizePreferences(session, prefsData.Prefs, _dependencies);

            prefsData.PrefsLoaded = true;

            var msg = new MsgPreferencesAndSettings();
            msg.Preferences = prefsData.Prefs;
            msg.Settings = new GameSettings
            {
                MaxCharacterSlots = MaxCharacterSlots
            };
            _netManager.ServerSendMessage(msg, session.Channel);
        }

        public void OnClientDisconnected(ICommonSession session)
        {
            _cachedPlayerPrefs.Remove(session.UserId);
        }

        public bool HavePreferencesLoaded(ICommonSession session)
        {
            return _cachedPlayerPrefs.ContainsKey(session.UserId);
        }


        /// <summary>
        /// Tries to get the preferences from the cache
        /// </summary>
        /// <param name="userId">User Id to get preferences for</param>
        /// <param name="playerPreferences">The user preferences if true, otherwise null</param>
        /// <returns>If preferences are not null</returns>
        public bool TryGetCachedPreferences(NetUserId userId,
            [NotNullWhen(true)] out PlayerPreferences? playerPreferences)
        {
            if (_cachedPlayerPrefs.TryGetValue(userId, out var prefs))
            {
                playerPreferences = prefs.Prefs;
                return prefs.Prefs != null;
            }

            playerPreferences = null;
            return false;
        }

        /// <summary>
        /// Retrieves preferences for the given username from storage.
        /// </summary>
        public PlayerPreferences GetPreferences(NetUserId userId)
        {
            var prefs = _cachedPlayerPrefs[userId].Prefs;
            if (prefs == null)
            {
                throw new InvalidOperationException("Preferences for this player have not loaded yet.");
            }

            return prefs;
        }

        /// <summary>
        /// Retrieves preferences for the given username from storage or returns null.
        /// </summary>
        public PlayerPreferences? GetPreferencesOrNull(NetUserId? userId)
        {
            if (userId == null)
                return null;

            if (_cachedPlayerPrefs.TryGetValue(userId.Value, out var pref))
                return pref.Prefs;
            return null;
        }

        private async Task<Preference> GetOrCreatePreferencesAsync(NetUserId userId, CancellationToken cancel)
        {
            var prefs = await _db.GetPlayerPreferencesAsync(userId, cancel);
            if (prefs is null)
            {
                var speciesToBlacklist =
                    new HashSet<string>(_cfg.GetCVar(CCVars.ICNewAccountSpeciesBlacklist).Split(","));
                return await _db.InitPrefsAsync(userId, HumanoidCharacterProfile.Random(speciesToBlacklist).AsEnabled(), cancel);
            }

            return prefs;
        }

        private PlayerPreferences SanitizePreferences(ICommonSession session, PlayerPreferences prefs, IDependencyCollection collection)
        {
            // Clean up preferences in case of changes to the game,
            // such as removed jobs still being selected.
            var prototypeManager = collection.Resolve<IPrototypeManager>();

            // Sanitize the job priorities
            // Far Horizons
            var priorities = new Dictionary<(ProtoId<FactionPrototype>, ProtoId<JobPrototype>), JobPriority>(
                prefs.JobPriorities
                .Where(p => 
                    prototypeManager.TryIndex(p.Key.faction, out var faction) &&
                    prototypeManager.TryIndex(p.Key.job, out var job) && 
                    faction.Playable &&
                    job.SetPreference && 
                    p.Value != JobPriority.Never
                    )
                );

            // Ensure only one high priority job
            var hasHighPrio = false;
            foreach (var (key, value) in priorities)
            {
                if (value != JobPriority.High)
                    continue;

                if (hasHighPrio)
                    priorities[key] = JobPriority.Medium;
                hasHighPrio = true;
            }

            return new PlayerPreferences(prefs.Characters.Select(p =>
            {
                return new KeyValuePair<int, HumanoidCharacterProfile>(p.Key, p.Value.Validated(session, collection));
            }), prefs.AdminOOCColor, prefs.ConstructionFavorites, priorities);
        }

        // public IEnumerable<KeyValuePair<NetUserId, HumanoidCharacterProfile>> GetSelectedProfilesForPlayers(
        //     List<NetUserId> usernames)
        // {
        //     return usernames
        //         .Select(p => (_cachedPlayerPrefs[p].Prefs, p))
        //         .Where(p => p.Prefs != null)
        //         .Select(p => new KeyValuePair<NetUserId, HumanoidCharacterProfile>(p.p, p.Prefs!.SelectedCharacter));
        // }

        internal static bool ShouldStorePrefs(LoginType loginType)
        {
            return loginType.HasStaticUserId();
        }

        private sealed class PlayerPrefData
        {
            public bool PrefsLoaded;
            public PlayerPreferences? Prefs;
        }

        void IPostInjectInit.PostInject()
        {
            _userDb.AddOnLoadPlayer(LoadData);
            _userDb.AddOnFinishLoad(FinishLoad);
            _userDb.AddOnPlayerDisconnect(OnClientDisconnected);
        }
    }
}
