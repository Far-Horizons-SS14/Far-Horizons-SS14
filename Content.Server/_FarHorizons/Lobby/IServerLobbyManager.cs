using Content.Shared._FarHorizons.Lobby;
using Content.Shared.GameTicking;
using Robust.Shared.Network;

namespace Content.Server._FarHorizons.Lobby;

public interface IServerLobbyManager : ISharedLobbyManager
{
    void RefreshJobPicks(Dictionary<NetUserId, PlayerGameStatus> players);

    void PreRoundStarted();
    void RoundStarted();
}