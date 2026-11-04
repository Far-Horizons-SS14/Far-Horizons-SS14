using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.PowerArmor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class PowerArmorPartComponent : Component
{
    /// <summary>
    /// How much damage goes to the user.
    /// </summary>
    [DataField(required: true)]
    public float DamageTransferModifier = 1.0f;

    /// <summary>
    /// The damage reduction when the part is unbroken.
    /// </summary>
    [DataField]
    public DamageModifierSet Modifiers = default!;

    /// <summary>
    /// The damage reduction when the part is broken
    /// </summary>
    [DataField(required: true)]
    public DamageModifierSet BrokenModifiers = default!;

    /// <summary>
    /// The speed penalty applied by this part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1.0f;

    /// <summary>
    /// The type of part this part is.
    /// </summary>
    [DataField(required: true)]
    public PowerArmorVisualLayers PartType = default;

    [DataField]
    public TimeSpan InstallTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The max integrity of a part.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 MaxIntegrity = FixedPoint2.Zero;

    /// <summary>
    /// Determines if the part is broken.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool isBroken = false;

    /// <summary>
    /// Is the part installed and if so to whom
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? AttachedTo;
}

[Serializable, NetSerializable]
public enum PowerArmorVisualLayers : byte
{
    UnderArmor,
    UnderArmorHelmet,
    Chest,
    Head,
    RArm,
    LArm,
    RLeg,
    LLeg
}