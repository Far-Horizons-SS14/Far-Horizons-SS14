using Content.Shared._FarHorizons.Factions;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Factions;

public interface IServerFactionManager : ISharedFactionManager {
    public void PostInit();

    /// <summary>
    /// Sets current faction to default if it's null, then returns current faction
    /// </summary>
    public FactionPrototype MustHaveCurrentFaction();
    /// <summary>
    /// Sets current faction, synchronizes current faction with clients
    /// </summary>
    public bool SetCurrentFaction(ProtoId<FactionPrototype>? faction);
    /// <summary>
    /// Attempts to guess which faction a job could belong to in the current round
    /// </summary>
    public ProtoId<FactionPrototype>? DecideFactionForJob(ProtoId<JobPrototype> job);
    /// <summary>
    /// Lists currently enabled factions (that can be selected via vote)
    /// </summary>
    public IEnumerable<FactionPrototype> ListEnabledFactions();
}