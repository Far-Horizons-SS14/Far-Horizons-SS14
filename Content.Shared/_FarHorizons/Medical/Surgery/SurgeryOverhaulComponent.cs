using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Medical.SurgeryOverhaul.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryBedSpeedComponent : Component
{
    [DataField]
    public float BedSpeedModifier = 2.0f;
}