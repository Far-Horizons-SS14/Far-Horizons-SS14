using System.Numerics;
using Content.Shared._FarHorizons.StarSystem.Helpers;
using Content.Shared._FarHorizons.StarSystem.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.StarSystem;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class StarSystemMapComponent : Component
{
    [ViewVariables, AutoNetworkedField] public int? Seed;
    [ViewVariables] public PlanetarySystem? StarSystem;
    [ViewVariables, AutoNetworkedField] public Vector2 StarOffset;
}

// Put this on any grid, set map Uid and dirty the component
// CC spawn does this automatically
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BecomesCustomStarSystemComponent : Component
{
    [ViewVariables, AutoNetworkedField] public EntityUid? Map;

    [DataField(required: true), ViewVariables, AutoNetworkedField]
    public ProtoId<CustomStarSystemPrototype> CustomSystem = default!;
}