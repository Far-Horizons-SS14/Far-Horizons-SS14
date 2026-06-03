using Content.Shared._FarHorizons.Lobby;
using Robust.Shared.Network;

namespace Content.Client._FarHorizons.Lobby;

public sealed partial class ClientLobbyManager : SharedLobbyManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;

    public override void Init()
    {
        base.Init();

        _netManager.RegisterNetMessage<MsgJobPicksUpdated>(ReceiveCurrentJobPicks);
    }

    private void ReceiveCurrentJobPicks(MsgJobPicksUpdated msg)
    {
        JobPicks = msg.JobPicks;
        CallOnOnJobPicksUpdated();
    }
}