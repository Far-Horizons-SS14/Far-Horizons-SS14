using Content.Shared.CCOperations.Systems;

namespace Content.Client._FarHorizons.CCOperations;

public sealed class CCOperationsSystem : SharedCCOperationsSystem
{
    public void ToggleAgentUplink(int agentId, bool status)
    {
        RaiseNetworkEvent(new CCAgentToggleUplinkMessage(agentId, status));
    }

    public void NeutralizeAgent(int agentId)
    {
        RaiseNetworkEvent(new CCNeutralizeAgentMessage(agentId));
    }
}