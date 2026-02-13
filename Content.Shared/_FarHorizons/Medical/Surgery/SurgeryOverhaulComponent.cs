using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.Chat.Prototypes;
using Content.Shared._FarHorizons.Research;

namespace Content.Shared._FarHorizons.Medical.SurgeryOverhaul.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryBedSpeedComponent : Component
{
    [DataField]
    public float BedSpeedModifier = 2.0f;
}
[RegisterComponent, NetworkedComponent] 
public sealed partial class OnFailDamageComponent : Component
{
    [DataField]
    public DamageSpecifier? Damage;
    [DataField]
    public ProtoId<EmotePrototype> Emote = "Scream";
}
[RegisterComponent, NetworkedComponent]
public sealed partial class NecrosisSurgeryTargetComponent : Component
{
    [DataField]
    public List<EntProtoId> RequiredSurgeries = new();
    [DataField("amount_of_surgeries")]
    public int AmountOfSurgeries = 1;
}
[RegisterComponent, NetworkedComponent] public sealed partial class DisableSurgeryComponent : Component;
[RegisterComponent, NetworkedComponent] public sealed partial class SurgeryAlterAppearanceComponent : Component;
[RegisterComponent, NetworkedComponent] public sealed partial class SurgeryRepairEyesComponent : Component;
[RegisterComponent, NetworkedComponent] public sealed partial class AnimalBypassComponent : Component;

// Valid Events

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryLimbExistConditionComponent : Component
{
    [DataField]
    public string Slot;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class RequireSpecificOrganicPartComponent : Component
{
    [DataField]
    public string Slot;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class RequireOrganicPartComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class RequireInorganicPartComponent : Component;

[RegisterComponent, NetworkedComponent] public sealed partial class NecrosisSurgeryComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class NecrosisSurgeryStepComponent : Component
{
    [DataField]
    public string Target = "bodypart";
    [DataField("seconds")]
    public double time = 120;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTechnologyComponent : Component
{
    [DataField(required: false)]
    public ProtoId<ResearchTreeUnlockFlagPrototype>? RequiredTechnology;
    
    [DataField]
    public Dictionary<ProtoId<ResearchTreeUnlockFlagPrototype>, long> TechnologyModifier = new();
}