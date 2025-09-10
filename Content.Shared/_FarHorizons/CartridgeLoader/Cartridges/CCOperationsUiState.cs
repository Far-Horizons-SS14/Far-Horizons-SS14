using Robust.Shared.Serialization;
using Content.Shared.CCOperations.Systems;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class CCOperationsUIState(List<CCOperativeAgent> agents) : BoundUserInterfaceState
{
    public List<CCOperativeAgent> Agents = agents;
}
