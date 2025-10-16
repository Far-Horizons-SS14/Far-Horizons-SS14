using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Research.Prototypes;

namespace Content.Shared._FarHorizons.Medical.SurgeryOverhaul.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryBedSpeedComponent : Component
{
    [DataField]
    public float BedSpeedModifier = 2.0f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryChemicalSpeedComponent : Component
{
    [DataField]
    public float ChemicalSpeedModifier = 1.0f;
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
public sealed partial class HealDamageComponent : Component
{
    [DataField]
    public DamageSpecifier? Heal;
    [DataField]
    public Dictionary<ProtoId<TechnologyPrototype>, float> TechnologyModifier = new();
}
[RegisterComponent, NetworkedComponent] 
public sealed partial class RequiredTechnologyComponent : Component
{
    [DataField]
    public ProtoId<TechnologyPrototype> Technology;
}
[RegisterComponent, NetworkedComponent] public sealed partial class SurgeryAlterAppearanceComponent : Component;