using System.Collections.Concurrent;
using System.Linq;
using Content.Server._FarHorizons.Implants;
using Content.Server.CartridgeLoader;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Server.GameTicking;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CCOperations.Systems;

namespace Content.Server._FarHorizons.CCOperations;

public sealed class CCOperationsSystem : SharedCCOperationsSystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;
    
    // Most probably not the ideal solution. but it shouldn't rely on in-game server machine ig
    private ConcurrentDictionary<int, CCOperativeAgent> _agents = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<CCUplinkImplantComponent, CCAgentInitializedEvent>(OnAgentInitialized);
        SubscribeLocalEvent<CCAgentUpdatedEvent>(OnAgentUpdated);
        SubscribeLocalEvent<CCOperationsCartridgeComponent, CartridgeUiReadyEvent>(OnCartridgeUiReady);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New is not GameRunLevel.PostRound)
            return;
        
        _agents.Clear();
    }

    private void OnAgentInitialized(EntityUid uid, CCUplinkImplantComponent component, ref CCAgentInitializedEvent ev)
    {
        _agents.TryAdd(ev.Agent.Id, ev.Agent);
    }

    private void OnAgentUpdated(CCAgentUpdatedEvent ev)
    {
        var agentId = ev.Agent.Id;
        if (_agents.TryGetValue(agentId, out var agent))
        {
            agent.Name = ev.Agent.Name;
            agent.JobTitle = ev.Agent.JobTitle;
            agent.UplinkOpen = ev.Agent.UplinkOpen;
            _agents[agentId] = agent;
        }
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

    public bool IsKnownAgent(EntityUid agentId) => _agents.ContainsKey(agentId.Id);

    public List<CCOperativeAgent> GetCCAgents() => _agents.Values.ToList();
}
