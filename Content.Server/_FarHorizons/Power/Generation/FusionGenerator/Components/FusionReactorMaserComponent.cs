using Content.Shared.Containers.ItemSlots;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent]
public sealed partial class FusionReactorMaserComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int PowerSetting = 0;

    [DataField]
    public int MaxPowerSetting = 5;

    [DataField]
    public float PowerExponent = 1.75f;

    [DataField]
    public float BasePower = 1e6f;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool InjectAntimatter = false;
    
    public const string AMJarSlotId = "jar_slot";
    [DataField(AMJarSlotId), ViewVariables]
    public ItemSlot AMJarSlot = new();
    
    [ViewVariables(VVAccess.ReadWrite)]
    public float Antimatter = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public float InjectionRate = 0.25f;
}