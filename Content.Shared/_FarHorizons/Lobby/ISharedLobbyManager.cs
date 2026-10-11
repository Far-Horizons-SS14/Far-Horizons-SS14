using Content.Shared._FarHorizons.Factions;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Lobby;

public interface ISharedLobbyManager
{
    void Init();
    void Shutdown();

    event Action? OnJobPicksUpdated;

    Dictionary<ProtoId<FactionJobAssignmentPrototype>, (int low, int med, int high)> GetJobPicks();
}