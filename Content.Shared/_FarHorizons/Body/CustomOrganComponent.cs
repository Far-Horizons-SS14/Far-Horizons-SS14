using Content.Shared.Body;
using Content.Shared.Damage;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Body;

[RegisterComponent, NetworkedComponent]
public sealed partial class VisionOrganRequiredForVisionComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class VisionOrganComponent : Component
{
    public int EyeDamage;
    public int MinDamage;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CustomOrganComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class HeadOrganComponent : Component
{
    public string NameBackup = "";
}
[RegisterComponent, NetworkedComponent]
public sealed partial class BrainExtraComponent : Component
{
    [DataField]
    public ComponentRegistry StoredComponents = new();
}

[RegisterComponent, NetworkedComponent]
public sealed partial class MovementOrganExpectedToMoveComponent : Component
{
    [DataField] public int ExpectedAmount = 2;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class MovementOrganComponent : Component
{
    [DataField] public float WalkSpeedModifier = 1;
    [DataField] public float SprintSpeedModifier = 1;
    [DataField] public bool ShoesNegate;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ConnectedOrganComponent : Component
{
    public const string ContainerID = "connected_organs";

    [ViewVariables] public Container? Organs;

    [DataField] public List<EntProtoId> Roundstart = new List<EntProtoId>();
}

[RegisterComponent, NetworkedComponent]
public sealed partial class VitalOrganComponent : Component
{
    [DataField] public DamageSpecifier Damage = new()
    {
        DamageDict = new ()
        {
            { "Bloodloss", 200 }, // Just enough to kill
        }
    }; 
}

[RegisterComponent]
public sealed partial class DetachedOrganVisualsExcludedComponent : Component;

[ByRefEvent]
public readonly record struct OnDisconnectedVisualOrganState;
[ByRefEvent]
public readonly record struct OnDisconnectedVisualMarkingsOrganState;

[ByRefEvent]
public readonly record struct BrainInserted(EntityUid Body);
[ByRefEvent]
public readonly record struct BrainRemoved(EntityUid Body);