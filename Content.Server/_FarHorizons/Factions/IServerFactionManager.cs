using Content.Shared.FarHorizons.Factions;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.FarHorizons.Factions;

public interface IServerFactionManager : ISharedFactionManager {
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
}