using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.LimbStatusEffect;

[RegisterComponent, NetworkedComponent]
public sealed partial class LimbStatusEffectManagerComponent : Component
{
    [ViewVariables] public Dictionary<string, ILimbStatusEffect> LastAppliedState = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LimbStatusEffectComponent : Component
{
    [ViewVariables, AutoNetworkedField] 
    public Dictionary<ProtoId<LimbStatusEffectPrototype>, TimeSpan?> Statuses = new();
    [DataField] public TimeSpan CleanupRate = TimeSpan.FromSeconds(1);
    [DataField] public TimeSpan NextUpdate = TimeSpan.Zero;
}
