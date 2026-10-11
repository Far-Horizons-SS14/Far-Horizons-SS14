using Content.Shared._FarHorizons.IPC.Traits;

namespace Content.Shared._FarHorizons.Silicons.IPC.Traits.Negative;

[RegisterComponent]
public sealed partial class MicroreactorIncompatibilityTraitComponent : IPCTraitComponent;

[RegisterComponent]
public sealed partial class HeavierFrameTraitComponent : IPCTraitComponent
{
    [DataField] public float SpeedModifier = 1.0f;
}

[RegisterComponent]
public sealed partial class IntegratedBatteryTraitComponent : IPCTraitComponent;