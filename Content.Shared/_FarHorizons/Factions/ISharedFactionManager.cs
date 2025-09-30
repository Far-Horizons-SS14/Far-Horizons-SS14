using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.FarHorizons.Factions;
public interface ISharedFactionManager
{
    public void Init();
    public void Shutdown();

    /// <summary>
    /// Event that gets invoked on both client and server whenever SetCurrentFaction() is called on server
    /// </summary>
    public event Action<ProtoId<FactionPrototype>?>? OnFactionUpdated;


    public FactionPrototype? GetCurrentFaction();
    public FactionPrototype GetDefaultFaction();
    public (FactionPrototype, JobPrototype) GetDefaultWithJob();
    public Color GetFactionColorOrDefault(FactionPrototype faction);

    public IEnumerable<FactionPrototype> ListFactions();
    public IEnumerable<FactionPrototype> ListPlayableFactions();
    public IEnumerable<FactionPrototype> ListSpawnableFactions();
    public IEnumerable<ProtoId<FactionPrototype>> ListSpawnableFactionIDs();
    public IEnumerable<FactionDepartmentAssignmentPrototype> ListFactionDepartments();
    public IEnumerable<FactionJobAssignmentPrototype> ListFactionJobs();

    public bool TryFindFaction(string search, out FactionPrototype? faction);

    // Overrides for JobPrototype
    public string OverrideLocalizedJobName(FactionJobAssignmentPrototype assignment);
    public string OverrideLocalizedJobName((ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype> job) factionJob);
    public string? OverrideLocalizedJobDescription(FactionJobAssignmentPrototype assignment);
    public string? OverrideLocalizedJobDescription((ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job) factionJob);
    public ProtoId<JobIconPrototype> OverrideJobIcon(FactionJobAssignmentPrototype assignment);
    public ProtoId<JobIconPrototype> OverrideJobIcon((ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype> job) factionJob);
    public ProtoId<StartingGearPrototype>? OverrideJobStartingGear(FactionJobAssignmentPrototype assignment);
    public ProtoId<StartingGearPrototype>? OverrideJobStartingGear((ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype> job) factionJob);
    public ProtoId<RoleLoadoutPrototype> OverrideJobLoadout(FactionJobAssignmentPrototype assignment);
    public ProtoId<RoleLoadoutPrototype> OverrideJobLoadout((ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job) factionJob);
    public EntProtoId? OverrideJobEntity(FactionJobAssignmentPrototype assignment);
    public EntProtoId? OverrideJobEntity((ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype> job) factionJob);
    public EntProtoId? OverrideJobPreviewEntity(FactionJobAssignmentPrototype assignment);
    public EntProtoId? OverrideJobPreviewEntity((ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype> job) factionJob);
}