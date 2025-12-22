using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._FarHorizons.ReagantDrain;

[RegisterComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ReagantDrainComponent : Component
{
    /// <summary>
    /// ReagentID for what solution to use as fuel.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> Fuel = "WeldingFuel";

    /// <summary>
    /// Solution name that can added to easily.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string SolutionContainer = "default";

    /// <summary>
    /// The solution on the <see cref="SolutionContainerManagerComponent"/> to use.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;
    
    /// <summary>
    /// Whether the combustion engine is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// How much fuel does the engine consume
    /// </summary>
    [DataField]
    public float DrainRate = 1f;

    /// <summary>
    /// When the next automatic fuel drain will occur
    /// </summary>
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// How long to wait between fuel draining
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
}
