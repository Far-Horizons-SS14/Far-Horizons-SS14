using System.Linq;
using Content.Server.Access.Components;
using Content.Server.Access.Systems;
using Content.Server._FarHorizons.Factions;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.PDA;
using Content.Server.Station.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Body;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Station;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Starlight.TextToSpeech; // Far Horizons
// Starlight Start
using Prometheus;
using Robust.Server.Containers;

// Starlight End

namespace Content.Server.Station.Systems;

/// <summary>
/// Manages spawning into the game, tracking available spawn points.
/// Also provides helpers for spawning in the player's mob.
/// </summary>
[PublicAPI]
public sealed class StationSpawningSystem : SharedStationSpawningSystem
{
    [Dependency] private readonly SharedAccessSystem _accessSystem = default!;
    [Dependency] private readonly ActorSystem _actors = default!;
    [Dependency] private readonly IdCardSystem _cardSystem = default!;
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly PdaSystem _pdaSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IServerFactionManager _factions = default!; // Far Horizons
    [Dependency] private readonly ContainerSystem _container = default!; // Far Horizons

    private List<CyberneticImplant> _allCybernetics = default!; // Starlight

    #region Starlight
    [Dependency] private readonly GameTicker _gameTicker = default!;
    private static readonly ProtoId<SpeciesPrototype> FallbackSpecies = "Human";
    private static readonly ProtoId<JobPrototype> FallbackJob = "Assistant";
    private static readonly Gauge _speciesJobsSpawns = Metrics.CreateGauge(
        "sl_species_jobs_spawns",
        "Contains info on species and jobs spawned at and during the round.",
        ["species", "job", "spawn_time"]
    );
    #endregion

    // Starlight
    public override void Initialize()
    {
        base.Initialize();
        _allCybernetics = CyberneticImplant.GetAllCybernetics(_prototypeManager);
    }

    /// <summary>
    /// Attempts to spawn a player character onto the given station.
    /// </summary>
    /// <param name="station">Station to spawn onto.</param>
    /// <param name="faction">The faction to assign, if any.</param>
    /// <param name="job">The job to assign, if any.</param>
    /// <param name="profile">The character profile to use, if any.</param>
    /// <param name="stationSpawning">Resolve pattern, the station spawning component for the station.</param>
    /// <returns>The resulting player character, if any.</returns>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    /// <remarks>
    /// This only spawns the character, and does none of the mind-related setup you'd need for it to be playable.
    /// </remarks>
    /// Far Horizons
    public EntityUid? SpawnPlayerCharacterOnStation(EntityUid? station, ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype>? job, HumanoidCharacterProfile? profile, StationSpawningComponent? stationSpawning = null)
    {
        if (station != null && !Resolve(station.Value, ref stationSpawning))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        var ev = new PlayerSpawningEvent(faction, job, profile, station); // Far Horizons

        RaiseLocalEvent(ev);
        DebugTools.Assert(ev.SpawnResult is { Valid: true } or null);

        return ev.SpawnResult;
    }

    //TODO: Figure out if everything in the player spawning region belongs somewhere else.
    #region Player spawning helpers

    /// <summary>
    /// Spawns in a player's mob according to their job and character information at the given coordinates.
    /// Used by systems that need to handle spawning players.
    /// </summary>
    /// <param name="coordinates">Coordinates to spawn the character at.</param>
    /// <param name="job">Job to assign to the character, if any.</param>
    /// <param name="profile">Appearance profile to use for the character.</param>
    /// <param name="station">The station this player is being spawned on.</param>
    /// <param name="entity">The entity to use, if one already exists.</param>
    /// <returns>The spawned entity</returns>
    public EntityUid SpawnPlayerMob(
        EntityCoordinates coordinates,
        ProtoId<FactionPrototype>? faction, // Far Horizons
        ProtoId<JobPrototype>? job,
        HumanoidCharacterProfile? profile,
        EntityUid? station,
        EntityUid? entity = null)
    {
        _prototypeManager.TryIndex(faction, out var factionProto); // Far Horizons
        _prototypeManager.TryIndex(job, out var prototype);
        RoleLoadout? loadout = null;

        // Need to get the loadout up-front to handle names if we use an entity spawn override.
        // Far Horizons override faction loadouts
        var jobLoadout = faction is null || job is null ? string.Empty : 
                            (string)_factions.OverrideJobLoadout((faction.Value, job.Value));

        if (_prototypeManager.TryIndex(jobLoadout, out RoleLoadoutPrototype? roleProto))
        {
            profile?.Loadouts.TryGetValue(jobLoadout, out loadout);

            // Set to default if not present
            if (loadout == null)
            {
                loadout = new RoleLoadout(jobLoadout);
                loadout.SetDefault(profile, _actors.GetSession(entity), _prototypeManager);
            }
        }

        // If we're not spawning a humanoid, we're gonna exit early without doing all the humanoid stuff.
        // Far Horizons - we don't check override job entity here, this is intentional, base job should have JobEntity to override it with faction.
        if (prototype?.JobEntity != null)
        {
            DebugTools.Assert(entity is null);
            // Far Horizons override job entity
            var jobEntity = Spawn(_factions.OverrideJobEntity((faction, prototype)), coordinates);
            _mindSystem.MakeSentient(jobEntity);

            // Make sure custom names get handled, what is gameticker control flow whoopy.
            if (loadout != null)
            {
                EquipRoleLoadout(jobEntity, loadout, roleProto!, profile); // Starlight edit
            }
            
            // Raise gear equipped event for non-humanoid jobs
            var jobEntityGearEv = new StartingGearEquippedEvent(jobEntity);
            RaiseLocalEvent(jobEntity, ref jobEntityGearEv);
            // Starlight End

            DoJobSpecials(job, jobEntity);
            _identity.QueueIdentityUpdate(jobEntity);
            return jobEntity;
        }

        string speciesId = profile != null ? profile.Species : HumanoidCharacterProfile.DefaultSpecies;

        if (!_prototypeManager.TryIndex<SpeciesPrototype>(speciesId, out var species))
            throw new ArgumentException($"Invalid species prototype was used: {speciesId}");

        entity ??= Spawn(species.Prototype, coordinates);

        SetupCybernetics(entity.Value, profile?.Cybernetics ?? []); // Starlight
        
        if (profile != null)
        {
            _visualBody.ApplyProfileTo(entity.Value, profile);
            _humanoidProfile.ApplyProfileTo(entity.Value, profile);
            _metaSystem.SetEntityName(entity.Value, profile.Name);

            // Far Horizons edit
            if (TryComp<TextToSpeechComponent>(entity.Value, out var ttsComp))
                ttsComp.Symspeech = profile.Symspeech ?? profile.DefaultSymspeech();

            //Starlight remove
            // if (profile.FlavorText != "" && _configurationManager.GetCVar(CCVars.FlavorText))
            // {
            //     AddComp<DetailExaminableComponent>(entity.Value).Content = profile.FlavorText;
            // }
        }

        // Starlight begin - we try to do a unified load of loadout and startinggear in one shot to
        // make it more consistent and equip things in a more effective order.
        if (loadout != null)
        {
            var startingGear = prototype?.StartingGear != null ? [_prototypeManager.Index<StartingGearPrototype>(prototype.StartingGear)] : Array.Empty<IEquipmentLoadout>();
            StarlightEquipRoleLoadout(entity.Value, loadout, startingGear, roleProto!);
        }
        else if (prototype?.StartingGear != null)
        {
            var startingGear = _prototypeManager.Index<StartingGearPrototype>(prototype.StartingGear);
            EquipStartingGear(entity.Value, startingGear, raiseEvent: false);
        }
        // Starlight end

        // if (loadout != null)
        // {
        //     EquipRoleLoadout(entity.Value, loadout, roleProto!, profile); // Starlight edit
        // }

        // if (prototype?.StartingGear != null)
        // {
        //     var startingGear = _prototypeManager.Index<StartingGearPrototype>(_factions.OverrideJobStartingGear((factionProto?.ID, prototype))!); // Far Horizons starting gear faction override
        //     EquipStartingGear(entity.Value, startingGear, raiseEvent: false);
        // }

        // Far Horizons species loadouts
        if (species.Loadout != null && _prototypeManager.TryIndex(species.Loadout.Value, out var speciesLoadoutProto) && profile != null && profile.SpeciesLoadout != null)
            EquipRoleLoadout(entity.Value, profile.SpeciesLoadout, speciesLoadoutProto);

        var gearEquippedEv = new StartingGearEquippedEvent(entity.Value);
        RaiseLocalEvent(entity.Value, ref gearEquippedEv);

        // Far Horizons
        if (prototype != null && factionProto != null && TryComp(entity.Value, out MetaDataComponent? metaData))
        {
            SetPdaAndIdCardData(entity.Value, metaData.EntityName, factionProto, prototype, station); // Far Horizons
        }

        DoJobSpecials(job, entity.Value);
        _identity.QueueIdentityUpdate(entity.Value);

        #region StarlightStats
        if (entity.HasValue)
        {
            if (!_prototypeManager.TryIndex(profile?.Species, out SpeciesPrototype? speciesProto))
            {
                speciesProto = _prototypeManager.Index(FallbackSpecies);
                Log.Warning($"Unable to find species {profile?.Species}, falling back to {FallbackSpecies}");
            }

            if (!_prototypeManager.TryIndex(job, out JobPrototype? jobProto))
            {
                jobProto = _prototypeManager.Index(FallbackJob);
                Log.Warning($"Unable to find job {job}, falling back to {FallbackJob}");
            }

            _speciesJobsSpawns
                .WithLabels(
                    Loc.GetString(speciesProto.Name),
                    _factions.OverrideLocalizedJobName((factionProto?.ID, jobProto)), // Far Horizons faction name override
                    _gameTicker.RunLevel.ToString())
                .Inc();
        }
        #endregion

        return entity.Value;
    }

    private void DoJobSpecials(ProtoId<JobPrototype>? job, EntityUid entity)
    {
        if (!_prototypeManager.Resolve(job, out JobPrototype? prototype))
            return;

        foreach (var jobSpecial in prototype.Special)
        {
            jobSpecial.AfterEquip(entity);
        }
    }

    /// Starlight
    /// <summary>
    /// Replaces humanoid's limbs with cybernetics on spawn
    /// </summary>
    private void SetupCybernetics(EntityUid entity, List<string> cybernetics){
        if (!TryComp(entity, out TransformComponent? transform) ||
            !TryComp(entity, out BodyComponent? bodyComp))
            return;
        
        if (bodyComp.Organs == null)
            return;

        Entity<TransformComponent, BodyComponent> body = (entity, transform, bodyComp);

        var installedCyberlimbs = _allCybernetics.Where(p => cybernetics.Contains(p.ID)).ToList();
        
        foreach (var implant in installedCyberlimbs){
            var implantEnt = _prototypeManager.Index<EntityPrototype>(implant.ID);

            var newPart = Spawn(implantEnt.ID, body.Comp1.Coordinates);
            if(!TryComp(newPart, out OrganComponent? organComp)){
                QueueDel(newPart);
                continue;
            }

            var oldParts = body.Comp2.Organs!.ContainedEntities.Where(p =>
                TryComp<OrganComponent>(p, out var organ) && organ.Category == organComp.Category).ToList();

            foreach (var oldPart in oldParts)
            {
                if (!TryComp(oldPart, out TransformComponent? oldPartTransform) ||
                    !TryComp(oldPart, out MetaDataComponent? oldPartMetadata))
                {
                    QueueDel(newPart);
                    break;
                }

                Entity<TransformComponent, MetaDataComponent> oldPartEnt = (oldPart, oldPartTransform, oldPartMetadata);

                _container.Remove(oldPartEnt.AsNullable(), body.Comp2.Organs, false, true);
                QueueDel(oldPart);
                _container.Insert(newPart, body.Comp2.Organs, body.Comp1, true);
            }
        }      
    }

    /// <summary>
    /// Sets the ID card and PDA name, job, and access data.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="characterName">Character name to use for the ID.</param>
    /// <param name="factionPrototype">Faction prototype to use for the PDA and ID.</param>
    /// <param name="jobPrototype">Job prototype to use for the PDA and ID.</param>
    /// <param name="station">The station this player is being spawned on.</param>
    /// Far Horizons
    public void SetPdaAndIdCardData(EntityUid entity, string characterName, FactionPrototype factionPrototype, JobPrototype jobPrototype, EntityUid? station)
    {
        if (!InventorySystem.TryGetSlotEntity(entity, "id", out var idUid))
            return;

        var cardId = idUid.Value;
        if (TryComp<PdaComponent>(idUid, out var pdaComponent) && pdaComponent.ContainedId != null)
            cardId = pdaComponent.ContainedId.Value;

        if (!TryComp<IdCardComponent>(cardId, out var card))
            return;

        // FarHorizons - custom job titles and faction overrides
        var jobTitle = _factions.OverrideLocalizedJobName((factionPrototype, jobPrototype));
        if (TryComp<PresetIdCardComponent>(cardId, out var presetIdCard) && presetIdCard.CustomJobTitle != null)
            jobTitle = presetIdCard.CustomJobTitle;
        
        _cardSystem.TryChangeFullName(cardId, characterName, card);
        _cardSystem.TryChangeJobTitle(cardId, jobTitle, card);

        // Far Horizons - faction icon override
        if (_prototypeManager.Resolve(_factions.OverrideJobIcon((factionPrototype, jobPrototype)), out var jobIcon))
            _cardSystem.TryChangeJobIcon(cardId, jobIcon, card);

        var extendedAccess = false;
        if (station != null)
        {
            var data = Comp<StationJobsComponent>(station.Value);
            extendedAccess = data.ExtendedAccess;
        }

        _accessSystem.SetAccessToJob(cardId, jobPrototype, extendedAccess);

        if (pdaComponent != null)
            _pdaSystem.SetOwner(idUid.Value, pdaComponent, entity, characterName);
    }

    #endregion Player spawning helpers
}

/// <summary>
/// Ordered broadcast event fired on any spawner eligible to attempt to spawn a player.
/// This event's success is measured by if SpawnResult is not null.
/// You should not make this event's success rely on random chance.
/// This event is designed to use ordered handling. You probably want SpawnPointSystem to be the last handler.
/// </summary>
[PublicAPI]
public sealed class PlayerSpawningEvent : EntityEventArgs
{
    /// <summary>
    /// The entity spawned, if any. You should set this if you succeed at spawning the character, and leave it alone if it's not null.
    /// </summary>
    public EntityUid? SpawnResult;
    /// <summary>
    /// The faction to use, if any.
    /// </summary>
    public readonly ProtoId<FactionPrototype>? Faction;
    /// <summary>
    /// The job to use, if any.
    /// </summary>
    public readonly ProtoId<JobPrototype>? Job;
    /// <summary>
    /// The profile to use, if any.
    /// </summary>
    public readonly HumanoidCharacterProfile? HumanoidCharacterProfile;
    /// <summary>
    /// The target station, if any.
    /// </summary>
    public readonly EntityUid? Station;

    // Far Horizons
    public PlayerSpawningEvent(ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype>? job, HumanoidCharacterProfile? humanoidCharacterProfile, EntityUid? station)
    {
        Faction = faction; // Far Horizons
        Job = job;
        HumanoidCharacterProfile = humanoidCharacterProfile;
        Station = station;
    }
}
