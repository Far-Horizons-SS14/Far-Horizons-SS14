using Content.Server._FarHorizons.Implants;
using Content.Server.CartridgeLoader;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CCOperations.Systems;
using Content.Shared.StationRecords;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.CCOperations;

public sealed class CCOperationsSystem : SharedCCOperationsSystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;
    private List<CCOperativeAgent> _agents = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CCUplinkImplantComponent, CCAgentInitializedEvent>(OnAgentInitialized);
        SubscribeLocalEvent<CCOperationsCartridgeComponent, CartridgeUiReadyEvent>(OnCartridgeUiReady);
    }

    private void OnAgentInitialized(EntityUid uid, CCUplinkImplantComponent component, ref CCAgentInitializedEvent ev)
    {
        var agent = new CCOperativeAgent(ev.AgentId.Id);
        _agents.Add(agent);
    }

    private void OnCartridgeUiReady(Entity<CCOperationsCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateReaderUi(args.Loader);
    }

    private void UpdateReaderUi(EntityUid loaderUid)
    {
        var state = new CCOperationsUIState(GetCCAgents());

        _cartridge.UpdateCartridgeUiState(loaderUid, state);
    }

    public List<CCOperativeAgent> GetCCAgents()
    {
        return _agents.ShallowClone();
    }
}
