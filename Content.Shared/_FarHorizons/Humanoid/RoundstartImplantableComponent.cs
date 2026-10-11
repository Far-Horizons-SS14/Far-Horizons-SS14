using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Humanoid;

[RegisterComponent, NetworkedComponent]
public sealed partial class RoundstartImplantableComponent : Component
{
    [DataField(required: true)] public int Cost;
    [DataField] public float IconScale = 1;
    [DataField] public Vector2 IconOffset = Vector2.Zero;
    [DataField] public List<LocId> Description = [];
}
