using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.PowerArmor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerArmorModuleComponent : Component
{
    /// <summary>
    /// How much time to install the module
    /// </summary>
    [DataField]
    public TimeSpan InstallTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Can this module be toggled?
    /// </summary>
    [DataField]
    public bool canBeToggled = true;

    /// <summary>
    /// Is it currently enabled.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool isEnabled = false;

    /// <summary>
    /// Passive power consumption for suit while suit is active.
    /// </summary>
    [DataField]
    public float IdlePowerDrain = 0f;

    /// <summary>
    /// Active power consumption for when the module is enabled and suit is powered
    /// </summary>
    [DataField]
    public float ActivePowerDrain = 0f;

    /// <summary>
    /// How much modding capacity is required to install
    /// </summary>
    [DataField]
    public int ComplexityCost = 1;

    [DataField]
    public bool addToOtherHalf = false;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class PowerArmorPassiveModuleComponent : Component
{   
    [DataField("comps")]
    public ComponentRegistry? Components;  
}