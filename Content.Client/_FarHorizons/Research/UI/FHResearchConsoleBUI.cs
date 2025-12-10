using System.Linq;
using Content.Shared._FarHorizons.Research;
using Content.Shared._FarHorizons.Research.Components;
using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._FarHorizons.Research.UI;

[UsedImplicitly]
public sealed class FHResearchConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private HashSet<ResearchTreeNodePrototype> _nodeProtos = [];
    private HashSet<ProtoId<ResearchTreeNodePrototype>> _unlockedNodes = [];
    private HashSet<ProtoId<ResearchTreeNodePrototype>> _researchedNodes = [];
    private HashSet<ProtoId<ResearchTreeTierPrototype>> _unlockedTiers = [];
    private bool _readonly = false;

    [ViewVariables]
    private FHResearchConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();
        
        _nodeProtos = [.. _protoMan.EnumeratePrototypes<ResearchTreeNodePrototype>()];
        _window = this.CreateWindow<FHResearchConsoleWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.OnServerButtonPressed += () => SendMessage(new ConsoleServerSelectionMessage());

        if (!_readonly)
        {
            _window.OnResearchButtonPressed += SendReseachRequest;
            _window.OnRemoveQueueButtonPressed += SendRemoveFromQueueRequest;
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is FHResearchConsoleBUIFullState fullState)
        {
            _nodeProtos = [.. fullState.Nodes.Select(p => _protoMan.Index(p))];
            _researchedNodes = fullState.ResearchedNodes;
            _unlockedTiers = fullState.UnlockedTiers;
            _unlockedNodes = fullState.UnlockedNodes;
            _readonly = fullState.Readonly;

            if (_readonly && _window != null)
            {
                _window.OnResearchButtonPressed -= SendReseachRequest;
                _window.OnRemoveQueueButtonPressed -= SendRemoveFromQueueRequest;
            }
            
            _window?.SetupUI(_nodeProtos, _unlockedTiers, _unlockedNodes, _researchedNodes, fullState.QueuedNodes, fullState.ResearchProgress, fullState.BankedPoints, fullState.Readonly);
        } else if (state is FHResearchConsoleBUIPartialState partialState)
        {
            _researchedNodes = partialState.ResearchedNodes;
            _unlockedTiers = partialState.UnlockedTiers;
            _unlockedNodes = partialState.UnlockedNodes;
            
            _window?.RefreshUI(_unlockedTiers, _unlockedNodes, _researchedNodes, partialState.QueuedNodes, partialState.ResearchProgress, partialState.BankedPoints);
        }
    }

    private void SendReseachRequest(ProtoId<ResearchTreeNodePrototype> node)
    {
        if (!_readonly)
            SendMessage(new FHResearchConsoleResearchRequest(node));
    }

    private void SendRemoveFromQueueRequest(ProtoId<ResearchTreeNodePrototype> node)
    {
        if (!_readonly)
            SendMessage(new FHResearchConsoleRemoveQueueRequest(node));
    }
}