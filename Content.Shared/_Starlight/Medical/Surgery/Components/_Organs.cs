using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared.Starlight.Medical.Surgery.Steps.Parts;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class EyeImplantComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class HandImplantComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class BrainImplantComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class OrganHeartComponent : Component;
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganTongueComponent : Component
{
    [DataField]
    public bool IsMuted;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class FunctionalOrganComponent : Component
{
    [DataField]
    public bool IsCybernetic = true;
    
    [DataField("comps")]
    public ComponentRegistry? Components;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StorageOrganComponent : Component
{    
    [DataField]
    public EntProtoId? OrganAction { get; set; }

    /// <summary>
    /// The action entity of the storage organ.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField]
    public string ActionKey;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PrivateStorageLimbComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> Limb = new();
}

/// <summary>
/// Used for opening the storage organ via action.
/// </summary>
public sealed partial class OpenStorageOrganEvent : InstantActionEvent 
{
    [DataField]
    public string Key = "InternalStorage";
}

[RegisterComponent, NetworkedComponent]
public sealed partial class OrganDamageComponent : Component
{
    [DataField]
    public DamageSpecifier? Damage;

    [ViewVariables] public DamageSpecifier? StoredDamage;
}
