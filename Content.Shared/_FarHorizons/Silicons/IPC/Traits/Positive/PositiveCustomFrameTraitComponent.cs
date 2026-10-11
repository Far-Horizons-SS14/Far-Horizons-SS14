using Content.Shared._FarHorizons.IPC.Traits;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Silicons.IPC.Traits.Positive;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyborgModuleTraitComponent : IPCTraitComponent
{
    [DataField(required: true), AutoNetworkedField]
    public string ModuleSlotId = string.Empty;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OverclockingTraitComponent : IPCToggleActionComponent
{
    [DataField] public float drawRateMultiplier = 2f;
    [DataField] public float speedModifier = 0.60f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RepairNanitesTraitComponent : IPCToggleActionComponent
{
    [ViewVariables] public DamageSpecifier oldDamage = new();
    [ViewVariables] public List<MobState> oldAllowedStates = new();
    [ViewVariables] public FixedPoint2 oldDamageCap = 0;
    [DataField] public float drawRateMultiplier = 3f;
    [DataField("damage")] public DamageSpecifier Damage = new();
    [DataField("allowedStates")] public List<MobState> AllowedStates = new();
    [DataField("damageCap")] public FixedPoint2 DamageCap = 0;
}

[RegisterComponent]
public sealed partial class BloodPoweredTraitComponent : IPCTraitComponent;
[RegisterComponent]
public sealed partial class LanguageDatabaseTraitComponent : IPCTraitComponent;