using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.VehicleBuckle.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleBuckleComponent : Component
{
    [DataField("unbuckletime")]
    public TimeSpan duration = TimeSpan.FromSeconds(3f);

    [DataField("dismountonstun")]
    public bool stundismount = true;

    [DataField("dismountonknockdown")]
    public bool knockdowndismount = true;
}