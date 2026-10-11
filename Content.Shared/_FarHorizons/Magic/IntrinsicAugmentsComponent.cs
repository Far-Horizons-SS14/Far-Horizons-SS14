using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Magic;

[RegisterComponent]
public sealed partial class IntrinsicAugmentsComponent : Component
{
    [DataField] public EntProtoId Action;
    [DataField] public ComponentRegistry AddComponents = new();
    [DataField] public Enum UiKey = StoreUiKey.Key;
    [DataField] public StoreComponent? Store;
}