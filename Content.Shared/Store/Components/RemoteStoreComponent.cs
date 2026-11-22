using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store.Components;

/// <summary>
/// This component manages a store which players can use to purchase different listings
/// through the ui. The currency, listings, and categories are defined in yaml.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RemoteStoreComponent : Component
{
    /// <summary>
    /// The store which is currently being targetted for remote opening
    /// </summary>
    [DataField]
    public EntityUid? Store;

    [DataField] public EntProtoId? Proto; // Far Horizons
    [DataField] public ProtoId<CurrencyPrototype>? Currency; // Far Horizons
}
