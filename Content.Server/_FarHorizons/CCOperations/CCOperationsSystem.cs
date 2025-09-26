using Content.Server._FarHorizons.Implants;
using Content.Shared.CCOperations.Systems;
using Content.Shared.StationRecords;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.CCOperations;

public sealed class CCOperationsSystem : SharedCCOperationsSystem
{
    private List<CCOperativeAgent> _ccOperatives = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CCUplinkImplantComponent, CCAgentInitializedEvent>(OnAgentInitialized);
    }

    private void OnAgentInitialized(EntityUid uid, CCUplinkImplantComponent component, ref CCAgentInitializedEvent ev)
    {
        var generalRecord = new GeneralStationRecord();
        var agent = new CCOperativeAgent((int)ev.AgentId, generalRecord);
        _ccOperatives.Add(agent);
    }

    public List<CCOperativeAgent> GetCCOperatives()
    {
        return _ccOperatives.ShallowClone();
    }
}
