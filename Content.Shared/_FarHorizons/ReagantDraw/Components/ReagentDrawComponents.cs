using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.ReagentDraw.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ReagentDrawComponent : Component
{
    /// <summary>
    /// ReagentID for what solution to whitelist.
    /// </summary>
    [DataField("whitelistedReagents")]
    public List<ProtoId<ReagentPrototype>> WhitelistedReagents = new();

    /// <summary>
    /// Solution container name
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string SolutionContainer = "default";

    /// <summary>
    /// The solution-carrying entity (either the inserted ent or a solution ent in its SolutionManager) to use.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;
    
    /// <summary>
    /// Whether the reagent drain is enabled
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// How much reagent is drained
    /// </summary>
    [DataField]
    public float DrainRate = 1f;

    /// <summary>
    /// When the next reagent drain will go off
    /// </summary>
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// How long between drains
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
}

/// <summary>
///     Raised when a reagent container's volume is changed
/// </summary>
[ByRefEvent]
public readonly record struct ReagentChangedEvent(float Volume, float MaxVolume);

/// <summary>
/// Raised directed on an entity when it no longer has any solution to draw from
/// </summary>
[ByRefEvent]
public readonly record struct ReagentContainerSlotEmptyEvent;
