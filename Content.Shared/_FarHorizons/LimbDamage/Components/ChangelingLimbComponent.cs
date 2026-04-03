using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.LimbDamage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingLimbComponent : Component
{
    [DataField] public FixedPoint2 DamageCap = 100;
}