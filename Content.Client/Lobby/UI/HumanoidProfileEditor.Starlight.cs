using System.Linq;
using Content.Client.Lobby.UI.Loadouts;
using Content.Shared._FarHorizons.Factions;
using Content.Shared._Starlight.Traits;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    public ProtoId<AntagPrototype>? AntagOverride;

    /// <summary>
    /// Track the state of the ShowClothes button to use for the profile preview
    /// </summary>
    public bool ShouldShowClothes => ShowClothes.Pressed;

    /// <summary>
    /// Called when trait selection changes in the TraitsTab.
    /// Updates the profile with the new trait selection.
    /// </summary>
    private void OnTraitsSelectionChanged(HashSet<ProtoId<TraitPrototype>> traits)
    {
        if (Profile is null)
            return;

        // Remove all existing traits - iterate directly over readonly collection
        foreach (var existingTrait in Profile.TraitPreferences)
        {
            Profile = Profile.WithoutTraitPreference(existingTrait, _prototypeManager);
        }

        // Add newly selected traits
        foreach (var trait in traits)
        {
            Profile = Profile.WithTraitPreference(trait.Id, _prototypeManager);
        }

        SetDirty();
    }

    /// <summary>
    /// Updates the traits tab with the current profile's selected traits.
    /// </summary>
    private void UpdateTraitsSelection()
    {
        if (Profile is null)
        {
            Traits.SetSelectedTraits(new HashSet<ProtoId<TraitPrototype>>(), Profile);
            return;
        }

        // Convert profile's trait preferences (strings) to ProtoId<TraitPrototype>
        var selectedTraits = new HashSet<ProtoId<TraitPrototype>>(Profile.TraitPreferences.Count);
        foreach (var traitId in Profile.TraitPreferences)
        {
            // Validate that the trait still exists in prototypes
            if (_prototypeManager.HasIndex(traitId))
            {
                selectedTraits.Add(new ProtoId<TraitPrototype>(traitId));
            }
        }

        Traits.SetSelectedTraits(selectedTraits, Profile);
        Traits.UpdateRequirements(Profile);
    }

    private void OpenAntagLoadout((FactionPrototype faction, AntagPrototype antag) antagProto, RoleLoadout roleLoadout, RoleLoadoutPrototype roleLoadoutProto)
    {
        _loadoutWindow?.Dispose();
        _loadoutWindow = null;
        var collection = IoCManager.Instance;

        if (collection == null || _playerManager.LocalSession == null || Profile == null)
            return;

        var session = _playerManager.LocalSession;

        _loadoutWindow = new LoadoutWindow(Profile, roleLoadout, roleLoadoutProto, session, collection)
        {
            Title = Loc.GetString("loadout-window-title-loadout", ("job", Loc.GetString(antagProto.antag.Name))),
        };

        _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
        _loadoutWindow.OpenCenteredLeft();

        _loadoutWindow.OnNameChanged += name =>
        {
            roleLoadout.EntityName = name;
            Profile = Profile.WithLoadout(roleLoadout);
            SetDirty();
        };

        _loadoutWindow.OnLoadoutPressed += (loadoutGroup, loadoutProto) =>
        {
            roleLoadout.AddLoadout(loadoutGroup, loadoutProto, _prototypeManager);
            _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
            Profile = Profile?.WithLoadout(roleLoadout);
            ReloadPreview();
        };

        _loadoutWindow.OnLoadoutUnpressed += (loadoutGroup, loadoutProto) =>
        {
            roleLoadout.RemoveLoadout(loadoutGroup, loadoutProto, _prototypeManager);
            _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
            Profile = Profile?.WithLoadout(roleLoadout);
            ReloadPreview();
        };

        AntagOverride = antagProto.antag.ID;
        JobOverride = antagProto.antag.PreviewStartingGear != null
            ? (antagProto.faction, _prototypeManager.EnumeratePrototypes<JobPrototype>().FirstOrDefault(j => j.StartingGear == antagProto.antag.PreviewStartingGear)!)
            : null;

        ReloadPreview();

        _loadoutWindow.OnClose += () =>
        {
            AntagOverride = null;
            JobOverride = null;
            ReloadPreview();
        };
    }

    private void SetCustomSpecieName(string customname)
    {
        Profile = Profile?.WithCustomSpecieName(customname);
        SetDirty();
    }
    
    private void UpdateCustomSpecieNameEdit()
    {
        var species = _species.Find(x => x.ID == Profile?.Species) ?? _species.First();
        CCustomSpecieNameEdit.Text = string.IsNullOrEmpty(Profile?.CustomSpecieName) ? Loc.GetString(species.Name) : Profile.CustomSpecieName;
        CCustomSpecieName.Visible = species.CustomName;
    }

    private void OnCyberneticsUpdated(List<CyberneticImplant> cybernetics)
    {
        Profile = Profile?.WithCybernetics(cybernetics.Select(p => p.ID).ToList());
        ReloadPreview();
    }

    private void UpdateSizeText()
    {
        if (Profile is null) return;
        if (_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var speciesPrototype))
        {
            var height = speciesPrototype.StandardSize * (Profile.Appearance.Height - 1f) * 2f + speciesPrototype.StandardSize;
            var weight = speciesPrototype.StandardWeight + speciesPrototype.StandardDensity * (Profile.Appearance.Width * Profile.Appearance.Height * Profile.Appearance.Height - 1);
            HeightDescribeLabel.Text = Loc.GetString("humanoid-profile-editor-height-label", ("height", Math.Round(height)));
            WidthDescribeLabel.Text = Loc.GetString("humanoid-profile-editor-width-label", ("weight", Math.Round(weight, 1)));
            _recordsTab?.UpdateComputedMetrics(Profile); // Chromatic Drift Records: Update the records tab with new computed metrics
        }
    }

    private void SetWidth(float newWidth)
    {
        if (Profile is null) return;
        Profile.Appearance = Profile.Appearance.WithWidth(newWidth);
        UpdateSizeText();
        ReloadProfilePreview();
    }

    private void SetHeight(float newHeight)
    {
        if (Profile is null) return;
        Profile.Appearance = Profile.Appearance.WithHeight(newHeight);
        UpdateSizeText();
        ReloadProfilePreview();
    }

    /// <summary>
    /// Updates selected job preferences to the priority selectors
    /// </summary>
    private void UpdateJobPreferences()
    {
        foreach (var ((factionID, jobId), prioritySelector) in _jobPriorities)
        {
            prioritySelector.Select((Profile?.JobPreferences.Contains((factionID, jobId)) ?? false) ? 0 : 1);
        }
    }

    private void UpdateSizeControls()
    {
        if (Profile == null) return;

        if (_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var speciesPrototype))
        {
            WidthSlider.MinValue = speciesPrototype.MinWidth;
            WidthSlider.MaxValue = speciesPrototype.MaxWidth;
            WidthSlider.Value = Profile.Appearance.Width;

            HeightSlider.MinValue = speciesPrototype.MinHeight;
            HeightSlider.MaxValue = speciesPrototype.MaxHeight;
            HeightSlider.Value = Profile.Appearance.Height;

            UpdateSizeText();
        }
    }

    private void UpdateCybernetics()
    {
        if (Profile is null)
        {
            return;
        }
        Cybernetics.SetData(Profile.Cybernetics, (_species.Find(x => x.ID == Profile?.Species) ?? _species.First()).RoundstartCyberwareCapacity);
    }
}