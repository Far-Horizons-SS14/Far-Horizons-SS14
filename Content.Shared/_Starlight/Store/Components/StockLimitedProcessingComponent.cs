using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Store.Components;

/// <summary>
/// This component was used to track which store listings are currently being processed,
/// but has since been obviated.
/// </summary>
[RegisterComponent]
public sealed partial class StockLimitedProcessingComponent : Component
{
}
