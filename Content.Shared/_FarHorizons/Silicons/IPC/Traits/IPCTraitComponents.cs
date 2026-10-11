using Content.Shared._FarHorizons.UI.BackgroundTraits;
using Content.Shared.FixedPoint;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.IPC.Traits;

public abstract partial class IPCTraitComponent : BackgroundTraitComponent;

public abstract partial class IPCPassiveTraitComponent : BackgroundPassiveTraitComponent;

public abstract partial class IPCActionTraitComponent : BackgroundActionTraitComponent;

public abstract partial class IPCToggleActionComponent : BackgroundToggleActionComponent;

[RegisterComponent]
public sealed partial class ModifyBloodstreamTraitComponent : IPCTraitComponent
{
    [DataField] public FixedPoint2 BloodRefreshRate = 0;
    [DataField] public float BloodReductionAmount = 0f;
}

[RegisterComponent]
public sealed partial class SetDamageModifierTraitComponent : IPCTraitComponent
{
    [DataField] public ProtoId<DamageModifierSetPrototype>? DamageModifierSetId;
}