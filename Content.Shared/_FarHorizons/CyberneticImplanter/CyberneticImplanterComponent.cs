using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.CyberneticImplanter;

[RegisterComponent, NetworkedComponent]
public sealed partial class CyberneticImplanterComponent : Component
{
    /// <summary>
    /// Sound played on Implant begin.
    /// </summary>
    [DataField]
    public SoundSpecifier? ImplantBeginSound = null;

    /// <summary>
    /// Sound played on Implant end.
    /// </summary>
    [DataField]
    public SoundSpecifier? ImplantEndSound = null;
}