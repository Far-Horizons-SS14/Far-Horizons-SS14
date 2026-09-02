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
    public float BasePower = 1e4f;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool InjectAntimatter = false;
    
    public const string AMJarSlotId = "jar_slot";
    [DataField(AMJarSlotId), ViewVariables]
    public ItemSlot AMJarSlot = new();
    
    /// <summary>
    /// Fractional parts of antimatter that could not be accounted for by the fuel jar.
    /// </summary>
    [ViewVariables]
    public float Antimatter = 0;

    /// <summary>
    /// Rate of amtimatter injection in units/s.
    /// </summary>
    /// <remarks>For reference, an AME set to an injection rate of 2 is 0.2u/s.</remarks>
    [DataField]
    public float InjectionRate = 1f;
}