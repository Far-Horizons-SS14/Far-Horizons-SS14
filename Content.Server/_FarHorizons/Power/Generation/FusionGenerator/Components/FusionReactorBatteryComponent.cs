using Content.Server._FarHorizons.Power.Generation.FusionGenerator.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Power.Components;

namespace Content.Server._FarHorizons.Power.Generation.FusionGenerator.Components;

[RegisterComponent]
public sealed partial class FusionReactorBatteryComponent : Component
{
    /// <summary>
    /// The power demand
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Demand = 0;

    /// <summary>
    /// How much power is being supplied
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Supply = 0;

    [ViewVariables]
    public PowerNetworkBatteryComponent NetBattery;

    [ViewVariables]
    public BatteryComponent Battery;

    /// <summary>
    /// A value used to assist in charge rate calculations
    /// </summary>
    [Access(typeof(FusionReactorSystem))]
    public float Cache = 0;
}