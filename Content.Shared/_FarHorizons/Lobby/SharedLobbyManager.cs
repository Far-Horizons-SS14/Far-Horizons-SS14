using Content.Shared._FarHorizons.Factions;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Lobby;

public abstract partial class SharedLobbyManager : ISharedLobbyManager
{
    protected Dictionary<ProtoId<FactionJobAssignmentPrototype>, (int, int, int)> JobPicks = [];

    public virtual void Init() { }

    public virtual void Shutdown() { }

    public event Action? OnJobPicksUpdated;
    protected void CallOnOnJobPicksUpdated() => OnJobPicksUpdated?.Invoke();

    public Dictionary<ProtoId<FactionJobAssignmentPrototype>, (int, int, int)> GetJobPicks() => JobPicks;
}