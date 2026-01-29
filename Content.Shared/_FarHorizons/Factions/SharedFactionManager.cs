using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Factions;

public partial class SharedFactionManager : ISharedFactionManager
{
    [Dependency] protected readonly IPrototypeManager _prototypeManager = default!;

    public static readonly string FallbackFaction = "FactionNT";

    public event Action<ProtoId<FactionPrototype>?>? OnFactionUpdated;

    protected ProtoId<FactionPrototype>? _currentFaction;
    protected ProtoId<FactionPrototype>? _defaultFaction;
    protected List<FactionPrototype> _factions = [];
    protected List<FactionDepartmentAssignmentPrototype> _factionDepartments = [];
    protected List<FactionJobAssignmentPrototype> _factionJobs = [];

    public virtual void Init() => _prototypeManager.PrototypesReloaded += ReloadPrototypes; // We cache all faction related prototypes, we need to knw when to reload them

    public virtual void Shutdown() => _prototypeManager.PrototypesReloaded -= ReloadPrototypes;

    private void ReloadPrototypes(PrototypesReloadedEventArgs obj){

        if (!obj.WasModified<FactionPrototype>() ||
            !obj.WasModified<FactionDepartmentAssignmentPrototype>() ||
            !obj.WasModified<FactionJobAssignmentPrototype>())
            return;

        PopulateCache();
    }

    private void PopulateCache(){
        _factions = [.. _prototypeManager.EnumeratePrototypes<FactionPrototype>()
                                        .OrderBy(p => p.Weight)
                                        .ThenBy(p => p.Name)];
        
        _factionDepartments = [.. _prototypeManager.EnumeratePrototypes<FactionDepartmentAssignmentPrototype>()
                                                .OrderBy(p => p.Weight)
                                                .ThenBy(p => p.ID)];
        
        _factionJobs = [.. _prototypeManager.EnumeratePrototypes<FactionJobAssignmentPrototype>()
                                            .OrderBy(p => p.Weight)
                                            .ThenBy(p => p.ID)];

        _defaultFaction = _factions.First(p => p.Default);
    }

    protected void CallOnFactionUpdated() => OnFactionUpdated?.Invoke(_currentFaction); // This is used on both client and server

    public FactionPrototype? GetCurrentFaction() =>
        ListFactions().FirstOrDefault(p => p != null && p.ID == _currentFaction, null);
    public FactionPrototype GetDefaultFaction()
    {
        if (_defaultFaction == null)
            PopulateCache();
        return ListFactions().First(p => p.ID == _defaultFaction);
    }
    public (FactionPrototype, JobPrototype) GetDefaultWithJob()
    {
        var (faction, job) = GetDefaultIdsWithJob();
        return (_factions.First(p => p.ID == faction), _prototypeManager.Index(job));
    }
    public Color GetFactionColorOrDefault(FactionPrototype faction) =>
        TryGetFactionColor(faction, out var color) ? color : Color.White;

    public IEnumerable<FactionPrototype> ListFactions(){
        if (_factions.Count == 0)
            PopulateCache();
        return _factions;
    }
    public IEnumerable<FactionPrototype> ListPlayableFactions() => 
        ListFactions().Where(p => p.Playable);
    public IEnumerable<FactionPrototype> ListSpawnableFactions() =>
        [.. ListPlayableFactions().Where(p => !p.Major || p == _currentFaction)];
    public IEnumerable<ProtoId<FactionPrototype>> ListSpawnableFactionIDs() =>
        ListSpawnableFactions().Select(p => (ProtoId<FactionPrototype>)p.ID);
    public IEnumerable<FactionDepartmentAssignmentPrototype> ListFactionDepartments(){
        if (_factionDepartments.Count == 0)
            PopulateCache();
        return _factionDepartments;
    }
    public IEnumerable<FactionJobAssignmentPrototype> ListFactionJobs(){
        if (_factionJobs.Count == 0)
            PopulateCache();
        return _factionJobs;
    }
    
    public bool TryFindFaction(string search, [NotNullWhen(true)] out FactionPrototype? faction){
        var found = ListPlayableFactions().Where(p => p.ID == search || p.Name == search || p.Alias.Contains(search)).ToList();
        faction = found.Count == 0 ? null : found.First();

        return faction != null;
    }  

    public string OverrideLocalizedJobName(FactionJobAssignmentPrototype assignment) =>
        assignment.Override == null || assignment.Override.Name == null ?
            _prototypeManager.Index(assignment.Job).LocalizedName :
            Loc.GetString(assignment.Override.Name);

    public string OverrideLocalizedJobName((ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype> job) factionJob) =>
        factionJob.faction is null || !TryGetJobAssignment((factionJob.faction.Value, factionJob.job), out var assignment) ? 
        _prototypeManager.Index(factionJob.job).LocalizedName :
        OverrideLocalizedJobName(assignment!);

    public string? OverrideLocalizedJobDescription(FactionJobAssignmentPrototype assignment) =>
        assignment.Override == null || assignment.Override.Description == null ?
            _prototypeManager.Index(assignment.Job).LocalizedDescription :
            Loc.GetString(assignment.Override.Description);

    public string? OverrideLocalizedJobDescription((ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job) factionJob) =>
        !TryGetJobAssignment(factionJob, out var assignment) ?
        _prototypeManager.Index(factionJob.job).LocalizedDescription :
        OverrideLocalizedJobDescription(assignment!);

    public ProtoId<JobIconPrototype> OverrideJobIcon(FactionJobAssignmentPrototype assignment) =>
        assignment.Override == null || assignment.Override.Icon == null ?
            _prototypeManager.Index(assignment.Job).Icon :
            assignment.Override.Icon.Value;

    public ProtoId<JobIconPrototype> OverrideJobIcon((ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype> job) factionJob) =>
        factionJob.faction == null || !TryGetJobAssignment((factionJob.faction.Value, factionJob.job), out var assignment) ? 
        _prototypeManager.Index(factionJob.job).Icon :
        OverrideJobIcon(assignment!);

    public ProtoId<StartingGearPrototype>? OverrideJobStartingGear(FactionJobAssignmentPrototype assignment) =>
        assignment.Override == null || assignment.Override.StartingGear == null ?
            _prototypeManager.Index(assignment.Job).StartingGear :
            assignment.Override.StartingGear;
    
    public ProtoId<StartingGearPrototype>? OverrideJobStartingGear((ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype> job) factionJob) =>
        factionJob.faction == null || !TryGetJobAssignment((factionJob.faction.Value, factionJob.job), out var assignment) ? 
        _prototypeManager.Index(factionJob.job).StartingGear :
        OverrideJobStartingGear(assignment!);
    
    public ProtoId<RoleLoadoutPrototype> OverrideJobLoadout(FactionJobAssignmentPrototype assignment) =>
        assignment.Override == null || assignment.Override.Loadout == null ?
            LoadoutSystem.GetJobPrototype(assignment.Job) :
            assignment.Override.Loadout.Value;
    
    public ProtoId<RoleLoadoutPrototype> OverrideJobLoadout((ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job) factionJob) =>
        !TryGetJobAssignment(factionJob, out var assignment) ?
        LoadoutSystem.GetJobPrototype(factionJob.job) :
        OverrideJobLoadout(assignment!);

    public EntProtoId? OverrideJobEntity(FactionJobAssignmentPrototype assignment) =>
        assignment.Override == null || assignment.Override.JobEntity == null ?
            _prototypeManager.Index(assignment.Job).JobEntity :
            assignment.Override.JobEntity;

    public EntProtoId? OverrideJobEntity((ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype> job) factionJob) =>
        factionJob.faction is null || !TryGetJobAssignment((factionJob.faction.Value, factionJob.job), out var assignment) ? 
        _prototypeManager.Index(factionJob.job).JobEntity :
        OverrideJobEntity(assignment!);
    
    public EntProtoId? OverrideJobPreviewEntity(FactionJobAssignmentPrototype assignment) =>
        assignment.Override == null || assignment.Override.JobPreviewEntity == null ?
            _prototypeManager.Index(assignment.Job).JobPreviewEntity :
            assignment.Override.JobPreviewEntity;

    public EntProtoId? OverrideJobPreviewEntity((ProtoId<FactionPrototype>? faction, ProtoId<JobPrototype> job) factionJob) =>
        factionJob.faction is null || !TryGetJobAssignment((factionJob.faction.Value, factionJob.job), out var assignment) ? 
        _prototypeManager.Index(factionJob.job).JobPreviewEntity :
        OverrideJobPreviewEntity(assignment!);

    private (ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job) GetDefaultIdsWithJob() =>
        (GetDefaultFaction(), SharedGameTicker.FallbackOverflowJob);
    private static bool TryGetFactionColor(FactionPrototype faction, out Color color) => Color.TryParse(faction.Color, out color);
    private FactionJobAssignmentPrototype? GetJobAssignment((ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job) factionJob) =>
        ListFactionJobs().FirstOrDefault(p => p.Faction == factionJob.faction && p.Job == factionJob.job);
    
    private bool TryGetJobAssignment((ProtoId<FactionPrototype> faction, ProtoId<JobPrototype> job) factionJob, out FactionJobAssignmentPrototype? assignment){
        assignment = GetJobAssignment(factionJob);
        return assignment != null;
    }
}