using Content.Shared.DeviceLinking;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._FarHorizons.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BallisticAmmoDistributorComponent : Component
{
    /// <summary>
    /// List of entities that will receive ammo from this entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<Entity<BallisticAmmoProviderComponent>> Receivers = [];

    /// <summary>
    /// The <see cref="BallisticAmmoProviderComponent"/> that contains the ammo to distribute.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public BallisticAmmoProviderComponent AmmoSource;

    /// <summary>
    /// How many bullets to transfer per update.
    /// </summary>
    [DataField]
    public int TransferAmount = 1;

    /// <summary>
    /// Does the distributor need to be anchored to function.
    /// </summary>
    [DataField]
    public bool RequireAnchor = true;

    [DataField("distributorConnectionPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string DistributorConnectionPort = "AmmoDistributorPort";
}

/// <summary>
/// Helper component that ensures whatever it's put on has a sink port.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BallisticAmmoReceiverComponent : Component
{
    [DataField("receiverConnectionPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string ReceiverConnectionPort = "AmmoReceiverPort";
}