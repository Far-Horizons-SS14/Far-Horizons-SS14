using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._FarHorizons.Research;
using Content.Shared._FarHorizons.Research.Components;
using Content.Shared.Chat;
using Content.Shared.Research.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Research;

public sealed partial class FHResearchSystem
{
    private void InitializeConsole()
    {
        SubscribeLocalEvent<FHResearchConsoleComponent, FHResearchConsoleResearchRequest>(OnResearchRequest);
        SubscribeLocalEvent<FHResearchConsoleComponent, FHResearchConsoleRemoveQueueRequest>(OnRemoveQueueRequest);
        SubscribeLocalEvent<FHResearchConsoleComponent, BeforeActivatableUIOpenEvent>(OnConsoleBeforeUiOpened);
        SubscribeLocalEvent<FHResearchConsoleComponent, ResearchRegistrationChangedEvent>(OnConsoleRegistrationChanged);
    }

    private void OnRemoveQueueRequest(Entity<FHResearchConsoleComponent> ent, ref FHResearchConsoleRemoveQueueRequest args)
    {
        if (TryGetServerWithTree(ent.Owner, out var server))
            RemoveResearchFromQueue((server.Value, server.Value.Comp), args.Node);
    }
    private void OnResearchRequest(Entity<FHResearchConsoleComponent> ent, ref FHResearchConsoleResearchRequest args)
    {
        if (TryGetServerWithTree(ent.Owner, out var server))
            AddResearchToQueue((server.Value, server.Value.Comp), args.Node);
    }
    private void OnConsoleRegistrationChanged(Entity<FHResearchConsoleComponent> ent, ref ResearchRegistrationChangedEvent args) =>
        UpdateUI((ent, ent.Comp), true);

    private void OnConsoleBeforeUiOpened(Entity<FHResearchConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args) =>
        UpdateUI((ent, ent.Comp), true);

    public void UpdateUI(Entity<FHResearchConsoleComponent?> ent, bool build = false)
    {
        if (!Resolve(ent, ref ent.Comp) || (!build && !_ui.IsUiOpen(ent.Owner, FHResearchConsoleUiKey.Key)))
            return;

        HashSet<ProtoId<ResearchTreeNodePrototype>> nodes = [];
        HashSet<ProtoId<ResearchTreeTierPrototype>> unlockedTiers = [];
        HashSet<ProtoId<ResearchTreeNodePrototype>> unlockedNodes = [];
        HashSet<ProtoId<ResearchTreeNodePrototype>> researchedNodes = [];
        List<ProtoId<ResearchTreeNodePrototype>> queuedNodes = [];
        Dictionary<ProtoId<ResearchTreeNodePrototype>, int> researchProgress = [];
        int bankedPoints = 0;

        if (TryGetServerWithTree(ent.Owner, out var server))
        {
            if (build)
                nodes = [.. GetTreeNodes(server.Value).Select(p => (ProtoId<ResearchTreeNodePrototype>)p.ID)];
            
            unlockedTiers = GetUnlockedTiers(server.Value);
            unlockedNodes = GetUnlockedNodes(server.Value);
            researchedNodes = server!.Value.Comp.Researched;
            queuedNodes = server!.Value.Comp.Queue;
            researchProgress = server!.Value.Comp.Progress;
            bankedPoints = server!.Value.Comp.BankedPoints;
        }

        if (build)
            _ui.SetUiState(ent.Owner, FHResearchConsoleUiKey.Key, new FHResearchConsoleBUIFullState(nodes, unlockedTiers, unlockedNodes, researchedNodes, queuedNodes, researchProgress, bankedPoints));
        else
            _ui.SetUiState(ent.Owner, FHResearchConsoleUiKey.Key, new FHResearchConsoleBUIPartialState(unlockedTiers, unlockedNodes, researchedNodes, queuedNodes, researchProgress, bankedPoints));
    }

    public void ShowError(Entity<FHResearchConsoleComponent?> ent, string message = "")
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        _audio.PlayPvs(ent.Comp.ErrorSound, ent);
        if (!string.IsNullOrEmpty(message))
            _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Speak, ChatTransmitRange.HideChat, true);
    }
}