using System.Linq;
using Content.Client.Station;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Shared._FarHorizons.Factions;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    /// <summary>
    /// A slim reload that only updates the entity itself and not any of the job entities, etc.
    /// </summary>
    private void ReloadHumanoidEntity(HumanoidCharacterProfile humanoid)
    {
        if (!EntMan.EntityExists(PreviewDummy) ||
            !EntMan.HasComponent<VisualBodyComponent>(PreviewDummy))
            return;

        EntMan.System<SharedVisualBodySystem>().ApplyProfileTo(PreviewDummy, humanoid);
    }

    /// <summary>
    /// Loads the profile onto a dummy entity.
    /// </summary>
    /// Far Horizons - heavily edited
    private void LoadHumanoidEntity(HumanoidCharacterProfile humanoid, (FactionPrototype faction, JobPrototype job)? job, bool jobClothes, ProtoId<AntagPrototype>? antagOverride)
    {
        ProfileName = humanoid.Name;
        JobName = null;
        LoadoutName = null;

        // Starlight Start: Antag Loadouts
        // If an antag override is provided, display that antag's loadout
        if (antagOverride != null && _prototypeManager.TryIndex(antagOverride.Value, out var antagProto))
        {
            PreviewDummy = EntMan.SpawnEntity(
                _prototypeManager.Index(humanoid.Species).DollPrototype,
                MapCoordinates.Nullspace);

            ReloadHumanoidEntity(humanoid);

            if (!jobClothes)
                return;

            JobName = Loc.GetString(antagProto.Name);

            // Then apply roleLoadout on top (which can override specific slots)
            if (antagProto.RoleLoadout != null && antagProto.RoleLoadout.Count > 0)
            {
                var antagLoadoutProtoId = antagProto.RoleLoadout.First();
                if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(antagLoadoutProtoId))
                {
                    var antagLoadout = humanoid.GetLoadoutOrDefault(
                        antagLoadoutProtoId,
                        _playerManager.LocalSession,
                        humanoid.Species,
                        EntMan,
                        _prototypeManager);

                    LoadoutName = GetLoadoutName(antagLoadout);
                    GiveDummyLoadout(antagLoadout);
                }
            }
            return;
        }
        // Starlight End

        EntProtoId? previewEntity = null;
        job ??= GetPreferredJob(humanoid);
        

        RoleLoadout? loadout;
        if (job is ({ } faction, { } jobProto))
        {
            try
            {
                loadout = humanoid.GetLoadoutOrDefault(
                    _factions.OverrideJobLoadout((faction, jobProto)),
                    _playerManager.LocalSession,
                    humanoid.Species,
                    EntMan,
                    _prototypeManager);
            }
            catch (UnknownPrototypeException)
            {
                loadout = new RoleLoadout();
            }

            previewEntity = _factions.OverrideJobPreviewEntity((faction, jobProto)) ??
                            _factions.OverrideJobEntity((faction, jobProto));

            if (previewEntity != null)
            {
                // Special type like borg or AI, do not spawn a human just spawn the entity.
                PreviewDummy = EntMan.SpawnEntity(previewEntity, MapCoordinates.Nullspace);
                JobName = _factions.OverrideLocalizedJobName((faction, jobProto));
                LoadoutName = GetLoadoutName(loadout);
                return;
            }
        }

        var dummy = _prototypeManager.Index(humanoid.Species).DollPrototype;
        PreviewDummy = EntMan.SpawnEntity(dummy, MapCoordinates.Nullspace);
        LoadCybernetics(humanoid);
        EntMan.System<SharedVisualBodySystem>().ApplyProfileTo(PreviewDummy, humanoid);

        // Bail now if all we need is the naked doll
        if (!jobClothes)
            return;
        
        // If we don't have an overridden job and the profile has NO job perefences, check for an antag preview
        if (job == null && humanoid.JobPreferences.Count == 0)
        {
            // Search the preferences for an antag with "PreviewStartingGear" defined
            foreach (var antag in humanoid.AntagPreferences)
            {
                if (!_prototypeManager.TryIndex(antag, out var selectedAntagProto))
                    continue;

                var antagLoadoutId = selectedAntagProto.RoleLoadout?.FirstOrDefault();

                // Brighteye Color Valid
                if (selectedAntagProto.PreviewStartingGear.HasValue || antagLoadoutId is not null)
                    if (selectedAntagProto.ID == "Brighteye")
                    {
                        humanoid.Appearance.EyeColor = EyeColor.MakeBrighteyeValid(humanoid.Appearance.EyeColor);
                        humanoid.Appearance.EyeGlowing = true;
                    }

                if (antagLoadoutId is not null)
                {
                    loadout = humanoid.GetLoadoutOrDefault(
                        antagLoadoutId,
                        _playerManager.LocalSession,
                        humanoid.Species,
                        EntMan,
                        _prototypeManager);

                    LoadoutName = GetLoadoutName(loadout);

                    GiveDummyLoadout(loadout);
                    JobName = Loc.GetString(selectedAntagProto.Name);
                    return;
                }

                if (selectedAntagProto.PreviewStartingGear.HasValue)
                {
                    // We found an antag to dress as! Set it and return.
                    GiveDummyAntagLoadout(selectedAntagProto);
                    JobName = Loc.GetString(selectedAntagProto.Name);
                    return;
                }
            }
        }

        if (job == null)
            // We STILL don't have a job, use fallback and don't set "JobName" (we don't want to display Passenger)
            job = _factions.GetDefaultWithJob();
        else
            JobName = _factions.OverrideLocalizedJobName((job.Value.faction, job.Value.job));

        GiveDummyJobClothes(humanoid, job.Value.job);

        loadout = humanoid.GetLoadoutOrDefault(
            _factions.OverrideJobLoadout((job.Value.faction, job.Value.job)),
            _playerManager.LocalSession,
            humanoid.Species,
            EntMan,
            _prototypeManager);

        LoadoutName = GetLoadoutName(loadout);

        GiveDummyLoadout(loadout);
    }

    /// <summary>
    /// Gets the highest priority job for the profile.
    /// </summary>
    private (FactionPrototype, JobPrototype)? GetPreferredJob(HumanoidCharacterProfile profile)
    {
        (ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job) highPriorityJob = default;
        if (profile.JobPreferences.Count == 1)
        {
            highPriorityJob = profile.JobPreferences.First();
        }
        else
        {
            var priorities = _preferencesManager.Preferences?.JobPriorities ?? [];
            foreach (var priority in new List<JobPriority> { JobPriority.High, JobPriority.Medium, JobPriority.Low })
            {
                highPriorityJob = profile.JobPreferences.FirstOrDefault(p => priorities.GetValueOrDefault(p) == priority);
                if (highPriorityJob.faction.Id != null && highPriorityJob.job.Id != null)
                    break;
            }
        }
        return highPriorityJob.faction.Id == null || highPriorityJob.job.Id == null
            ? null
            : (_prototypeManager.Index(highPriorityJob.faction), _prototypeManager.Index(highPriorityJob.job));
    }

    private void GiveDummyLoadout(RoleLoadout? roleLoadout)
    {
        if (roleLoadout == null)
            return;

        var spawnSys = EntMan.System<StationSpawningSystem>();

        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in group)
            {
                if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                    continue;

                spawnSys.EquipStartingGear(PreviewDummy, loadoutProto);
            }
        }
    }

    /// <summary>
    /// Applies the specified job's clothes to the dummy.
    /// </summary>
    private void GiveDummyJobClothes(HumanoidCharacterProfile profile, JobPrototype job)
    {
        var inventorySys = EntMan.System<InventorySystem>();
        if (!inventorySys.TryGetSlots(PreviewDummy, out var slots))
            return;

        // Apply loadout
        if (profile.Loadouts.TryGetValue(job.ID, out var jobLoadout))
        {
            foreach (var loadouts in jobLoadout.SelectedLoadouts.Values)
            {
                foreach (var loadout in loadouts)
                {
                    if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                        continue;

                    // TODO: Need some way to apply starting gear to an entity and replace existing stuff coz holy fucking shit dude.
                    foreach (var slot in slots)
                    {
                        // Try startinggear first
                        if (_prototypeManager.Resolve(loadoutProto.StartingGear, out var loadoutGear))
                        {
                            var itemType = ((IEquipmentLoadout) loadoutGear).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                EntMan.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
                            }
                        }
                        else
                        {
                            var itemType = ((IEquipmentLoadout) loadoutProto).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                EntMan.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
                            }
                        }
                    }
                }
            }
        }

        if (!_prototypeManager.Resolve(job.StartingGear, out var gear))
            return;

        foreach (var slot in slots)
        {
            var itemType = ((IEquipmentLoadout) gear).GetGear(slot.Name);

            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
            {
                EntMan.DeleteEntity(unequippedItem.Value);
            }

            if (itemType != string.Empty)
            {
                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
            }
        }
    }

    /// <summary>
    /// Apply PreviewStartingGear from antag prototype to the dummy.
    /// </summary>
    private void GiveDummyAntagLoadout(AntagPrototype antag)
    {
        if (!antag.PreviewStartingGear.HasValue)
            return;

        var spawnSys = EntMan.System<StationSpawningSystem>();

        spawnSys.EquipStartingGear(PreviewDummy, antag.PreviewStartingGear);
    }

    private string? GetLoadoutName(RoleLoadout loadout)
    {
        if (_prototypeManager.TryIndex(loadout.Role, out var roleLoadoutPrototype) &&
            roleLoadoutPrototype.CanCustomizeName)
            return loadout.EntityName;
        return null;
    }
}
