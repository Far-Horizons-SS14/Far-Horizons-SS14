using System.Linq;
using System.Numerics;
using Content.Client.Lobby.UI.Loadouts;
using Content.Client.Lobby.UI.Roles;
using Content.Shared._FarHorizons.Factions;
using Content.Shared.Clothing;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{

    /// <summary>
    /// Temporary override of their selected job, used to preview roles.
    /// </summary>
    /// Far Horizons
    public (FactionPrototype, JobPrototype)? JobOverride;

    // One at a time.
    private LoadoutWindow? _loadoutWindow;

    // Far Horizons
    private readonly List<((ProtoId<FactionPrototype>, ProtoId<JobPrototype>), RequirementsSelector)> _jobPriorities = new();

    // Far Horizons
    private readonly Dictionary<(ProtoId<FactionPrototype>, ProtoId<DepartmentPrototype>), BoxContainer> _jobCategories = new();

    /// <summary>
    /// Updates selected job priorities to the profile's.
    /// </summary>
    /// Far Horizons removed
    // private void UpdateJobPriorities()
    // {
    //     foreach (var (jobId, prioritySelector) in _jobPriorities)
    //     {
    //         var priority = Profile?.JobPriorities.GetValueOrDefault(jobId, JobPriority.Never) ?? JobPriority.Never;
    //         prioritySelector.Select((int)priority);
    //     }
    // }

    /// <summary>
    /// Refresh all loadouts.
    /// </summary>
    public void RefreshLoadouts()
    {
        _loadoutWindow?.Dispose();
    }

    private void OpenLoadout((FactionPrototype faction, JobPrototype job) factionJob, RoleLoadout roleLoadout, RoleLoadoutPrototype roleLoadoutProto)
    {
        _loadoutWindow?.Dispose();
        _loadoutWindow = null;
        var collection = IoCManager.Instance;

        if (collection == null || _playerManager.LocalSession == null || Profile == null)
            return;

        JobOverride = factionJob;
        var session = _playerManager.LocalSession;

        _loadoutWindow = new LoadoutWindow(Profile, roleLoadout, roleLoadoutProto, _playerManager.LocalSession, collection)
        {
            Title = Loc.GetString("loadout-window-title-loadout", ("job", $"{_factions.OverrideLocalizedJobName((factionJob.faction, factionJob.job))}")),
        };

        // Refresh the buttons etc.
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

        JobOverride = factionJob;
        ReloadPreview();

        _loadoutWindow.OnClose += () =>
        {
            JobOverride = null;
            ReloadPreview();
        };

        if (Profile is null)
            return;

        UpdateJobPreferences();
    }

    /// <summary>
    /// Refreshes all job selectors.
    /// </summary>
    public void RefreshJobs()
    {
        JobList.RemoveAllChildren();
        _jobCategories.Clear();
        _jobPriorities.Clear();

        var items = new[]
        {
            ("humanoid-profile-editor-antag-preference-yes-button", 0),
            ("humanoid-profile-editor-antag-preference-no-button", 1)
        };

        // Far Horizons - factions
        Dictionary<ProtoId<FactionPrototype>, BoxContainer> faction_tabs = new();
        foreach (var faction in _factions.ListPlayableFactions().ToList())
        {
            var faction_tab = new ScrollContainer
            {
                VerticalExpand = true,
                Name = faction.Name,
                ToolTip = Loc.GetString(faction.Description)
            };

            JobList.AddChild(faction_tab);

            var faction_tab_content = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
            };


            faction_tab.AddChild(faction_tab_content);

            faction_tabs.Add(faction, faction_tab_content);
        }

        foreach (var dptAssignment in _factions.ListFactionDepartments()
                                        .Where(p => _factions.ListPlayableFactions()
                                            .Any(e => e.ID == p.Faction))
                                            .OrderBy(p => p.Weight)
                                            .ThenBy(p => p.Department))
        {
            if (!_prototypeManager.TryIndex<DepartmentPrototype>(dptAssignment.Department, out var department) ||
                !_prototypeManager.TryIndex<FactionPrototype>(dptAssignment.Faction, out var faction))
                continue;

            var departmentName = Loc.GetString(dptAssignment.NameOverride != null && dptAssignment.NameOverride != "" ? 
                                                dptAssignment.NameOverride : 
                                                department.Name);

            if (!_jobCategories.TryGetValue((faction.ID, department.ID), out var category))
            {
                category = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Name = dptAssignment.Department,
                    ToolTip = Loc.GetString("humanoid-profile-editor-jobs-amount-in-department-tooltip",
                        ("departmentName", departmentName))
                };

                category.AddChild(new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#464966") },
                    Children =
                {
                    new Label
                    {
                        Text = Loc.GetString("humanoid-profile-editor-department-jobs-label",
                            ("departmentName", departmentName)),
                        Margin = new Thickness(5f, 0, 0, 0)
                    }
                }
                });

                _jobCategories[(dptAssignment.Faction, dptAssignment.Department)] = category;
                faction_tabs[dptAssignment.Faction].AddChild(category);
            }

            foreach (var jobAssignment in _factions.ListFactionJobs()
                                                .Where(p => (p.Faction == faction) && department.Roles.Contains(p.Job))
                                                .OrderBy(p => p.Weight)
                                                .ThenBy(p => p.Job))
            {
                if (!_prototypeManager.TryIndex<JobPrototype>(jobAssignment.Job, out var job))
                    continue;

                var jobContainer = new BoxContainer()
                {
                    Orientation = LayoutOrientation.Horizontal,
                };

                var selector = new RequirementsSelector()
                {
                    Margin = new Thickness(3f, 3f, 3f, 0f),
                };
                selector.OnOpenGuidebook += OnOpenGuidebook;

                var icon = new TextureRect
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center
                };
                var jobIcon = _prototypeManager.Index(_factions.OverrideJobIcon(jobAssignment));
                icon.Texture = _sprite.Frame0(jobIcon.Icon);

                selector.Setup(items, _factions.OverrideLocalizedJobName(jobAssignment), 200, _factions.OverrideLocalizedJobDescription(jobAssignment), icon, job.Guides);

                if (!_requirements.IsAllowed(job, Profile, out var reason))
                {
                    selector.LockRequirements(reason);
                    Profile = Profile?.WithoutJob(faction, job);
                    SetDirty();
                }
                else
                {
                    selector.UnlockRequirements();
                }

                selector.OnSelected += selection =>
                {
                    var include = selection == 0;
                    Profile = Profile?.WithJob(faction, job, include);

                    UpdateJobPreferences();
                    ReloadPreview();
                    SetDirty();
                };

                var loadoutWindowBtn = new Button()
                {
                    Text = Loc.GetString("loadout-window"),
                    HorizontalAlignment = HAlignment.Right,
                    VerticalAlignment = VAlignment.Center,
                    Margin = new Thickness(3f, 3f, 0f, 0f),
                };

                var collection = IoCManager.Instance!;
                var protoManager = collection.Resolve<IPrototypeManager>();

                // If no loadout found then disabled button
                if (!protoManager.TryIndex<RoleLoadoutPrototype>(_factions.OverrideJobLoadout(jobAssignment), out var roleLoadoutProto))
                {
                    loadoutWindowBtn.Disabled = true;
                }
                // else
                else
                {
                    loadoutWindowBtn.OnPressed += args =>
                    {
                        RoleLoadout? loadout = null;

                        // Clone so we don't modify the underlying loadout.
                        Profile?.Loadouts.TryGetValue(_factions.OverrideJobLoadout(jobAssignment), out loadout);
                        loadout = loadout?.Clone();

                        if (loadout == null)
                        {
                            loadout = new RoleLoadout(roleLoadoutProto.ID);
                            loadout.SetDefault(Profile, _playerManager.LocalSession, _prototypeManager);
                        }

                        OpenLoadout((faction, job), loadout, roleLoadoutProto);
                    };
                }

                _jobPriorities.Add(((jobAssignment.Faction, job.ID), selector));
                jobContainer.AddChild(selector);
                jobContainer.AddChild(loadoutWindowBtn);
                category.AddChild(jobContainer);
            }

            faction_tabs[dptAssignment.Faction].AddChild(new Control
            {
                MinSize = new Vector2(0, 23),
            });
        }

        UpdateJobPreferences();
    }

    public void RefreshAntags()
    {
        AntagList.RemoveAllChildren();
        var items = new[]
        {
            ("humanoid-profile-editor-antag-preference-yes-button", 0),
            ("humanoid-profile-editor-antag-preference-no-button", 1)
        };

        foreach (var antag in _prototypeManager.EnumeratePrototypes<AntagPrototype>().OrderBy(a => Loc.GetString(a.Name)))
        {
            if (!antag.SetPreference)
                continue;

            var antagContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
            };

            var selector = new RequirementsSelector()
            {
                Margin = new Thickness(3f, 3f, 3f, 0f),
            };
            selector.OnOpenGuidebook += OnOpenGuidebook;

            var title = Loc.GetString(antag.Name);
            var description = Loc.GetString(antag.Objective);
            selector.Setup(items, title, 250, description, guides: antag.Guides);
            selector.Select(Profile?.AntagPreferences.Contains(antag.ID) == true ? 0 : 1);

            if (!_requirements.IsAllowed(
                    antag,
                    // Starlight start - change how antag requirement check interacts with profiles
                    //(HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter,
                    Profile,
                    // Starlight end
                    out var reason))
            {
                selector.LockRequirements(reason);
                Profile = Profile?.WithAntagPreference(antag.ID, false);
                SetDirty();
            }
            else
            {
                selector.UnlockRequirements();
            }

            selector.OnSelected += preference =>
            {
                Profile = Profile?.WithAntagPreference(antag.ID, preference == 0);
                ReloadPreview();
                SetDirty();
            };

            antagContainer.AddChild(selector);

            var loadoutWindowBtn = new Button() // Starlight edit: Antag loadouts
            {
                // Disabled = true, // Starlight edit: Antag loadouts
                Text = Loc.GetString("loadout-window"),
                HorizontalAlignment = HAlignment.Right,
                Margin = new Thickness(3f, 0f, 0f, 0f),
            }; // Starlight edit: Antag loadouts

            // Starlight Start: Antag loadouts
            var antagLoadoutId = antag.RoleLoadout?.FirstOrDefault();

            if (antagLoadoutId == null || !_prototypeManager.TryIndex<RoleLoadoutPrototype>(antagLoadoutId.Value, out var roleLoadoutProto))
            {
                loadoutWindowBtn.Disabled = true;
            }
            else
            {
                loadoutWindowBtn.OnPressed += _ =>
                {
                    RoleLoadout? loadout = null;

                    Profile?.Loadouts.TryGetValue(roleLoadoutProto.ID, out loadout);
                    loadout = loadout?.Clone();

                    if (loadout == null)
                    {
                        loadout = new RoleLoadout(roleLoadoutProto.ID);
                        loadout.SetDefault(Profile, _playerManager.LocalSession, _prototypeManager, force: true);
                    }

                    OpenAntagLoadout((_factions.GetDefaultFaction(), antag), loadout, roleLoadoutProto);
                };
            }

            antagContainer.AddChild(loadoutWindowBtn);
            // Starlight ENd

            AntagList.AddChild(antagContainer);
        }
    }
}
