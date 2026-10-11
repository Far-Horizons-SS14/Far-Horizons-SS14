using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.UI.BackgroundTraits;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BackgroundTraitsComponent : Component
{
    [DataField] public EntProtoId TraitsAction = "ActionOpenVampireTraits";
    [DataField] public Enum TraitsUiKey = BackgroundTraitsUiKey.Key;
    [DataField, AutoNetworkedField] public string WindowName = "Traits Menu";
    [DataField, AutoNetworkedField] public List<ProtoId<BackgroundTraitPrototype>> Traits = new();
    [DataField] public bool AllowTraitSelection = true;
    [ViewVariables(VVAccess.ReadOnly)] public List<ProtoId<BackgroundTraitPrototype>>? SelectedTraits;
}

public abstract partial class BackgroundTraitComponent : Component;

public abstract partial class BackgroundPassiveTraitComponent : BackgroundTraitComponent
{
    [DataField] public TimeSpan TickRate = TimeSpan.Zero;
    [ViewVariables(VVAccess.ReadOnly)] public TimeSpan NextUpdate = TimeSpan.Zero;
}

public abstract partial class BackgroundActionTraitComponent : BackgroundTraitComponent
{
    [DataField(required: true)] public EntProtoId Action;
}

public abstract partial class BackgroundToggleActionComponent : BackgroundActionTraitComponent
{
    [DataField, AutoNetworkedField] public bool Toggled;
}