using Content.Shared.FarHorizons.Factions;
using Robust.Shared.Network;

namespace Content.Client.FarHorizons.Factions;

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