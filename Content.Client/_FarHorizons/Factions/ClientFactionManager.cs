using Content.Shared._FarHorizons.Factions;
using Robust.Shared.Network;

namespace Content.Client._FarHorizons.Factions;

public sealed partial class ClientFactionManager : SharedFactionManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;

    public override void Init()
    {
        base.Init();

        _netManager.RegisterNetMessage<MsgFactionSelected>(ReceiveCurrentFactions);
    }

    private void ReceiveCurrentFactions(MsgFactionSelected msg){
        _currentFaction = msg.Faction;
        CallOnFactionUpdated();
    }
}