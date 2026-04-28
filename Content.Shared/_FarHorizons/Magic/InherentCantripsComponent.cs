using Content.Shared.Actions;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Magic;

[RegisterComponent]
public sealed partial class InherentCantripsComponent : Component
{
    [DataField] public EntProtoId Action;
    [DataField] public ComponentRegistry AddComponents = new();
    [DataField] public Enum UiKey = StoreUiKey.Key;
    [DataField] public StoreComponent? Store;
}

public sealed partial class OpenInherentCantripsEvent : InstantActionEvent;