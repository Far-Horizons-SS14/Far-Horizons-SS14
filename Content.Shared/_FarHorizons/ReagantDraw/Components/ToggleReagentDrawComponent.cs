using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.ReagentDraw;

/// <summary>
/// Integrate ReagentDraw and ItemToggle.
/// Make toggling this item require reagents, and deactivates the item when reagents runs out.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ToggleReagentDrawComponent : Component;
