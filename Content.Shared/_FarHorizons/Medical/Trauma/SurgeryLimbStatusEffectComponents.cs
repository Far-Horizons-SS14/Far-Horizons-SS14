using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Medical.Trauma;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryLimbStatusMinCapBleedComponent : Component
{
    [DataField] public TimeSpan RefreshRate = TimeSpan.FromSeconds(0.5);
    [ViewVariables] public TimeSpan NextUpdate = TimeSpan.Zero;
    [DataField, AutoNetworkedField] public int MinBleedCap;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryLimbStatusMinDamageCapComponent : Component
{
    [DataField] public TimeSpan RefreshRate = TimeSpan.FromSeconds(0.5);
    [ViewVariables] public TimeSpan NextUpdate = TimeSpan.Zero;
    [DataField, AutoNetworkedField] public DamageSpecifier MinDamageCap = new();
    [DataField, AutoNetworkedField] public FixedPoint2 DamagePerTick = FixedPoint2.New(0.5);
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryLimbStatusDamageOnMoveComponent : Component
{
    [DataField, AutoNetworkedField] public Dictionary<ProtoId<OrganCategoryPrototype>, DamageSpecifier> Damage = new();
    [DataField] public TimeSpan DamageRate = TimeSpan.FromSeconds(1);
    [ViewVariables] public TimeSpan NextDamage = TimeSpan.Zero;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryLimbStatusBlindComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryLimbStatusMuteComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryLimbStatusLimbDisabledComponent : Component;