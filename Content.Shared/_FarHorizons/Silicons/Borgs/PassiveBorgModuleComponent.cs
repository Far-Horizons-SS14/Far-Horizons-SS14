using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Damage.Prototypes;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// Handles the logic for passive borg modules mostly for what type it is.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBorgSystem))] 
public sealed partial class PassiveBorgModuleComponent : Component
{
    [DataField] public PassiveBorgModuleType PassiveType = PassiveBorgModuleType.None;
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBorgSystem))] 
public sealed partial class ArmorBorgModuleComponent : Component
{
    [DataField("damageModifierSet")]
    public ProtoId<DamageModifierSetPrototype>? DamageModifierSetId;
}

[Serializable, NetSerializable]
public enum PassiveBorgModuleType
{
    None = 0,
    Access = 1 << 0,
    Armor = 1 << 1,
    Speed = 1 << 2
}