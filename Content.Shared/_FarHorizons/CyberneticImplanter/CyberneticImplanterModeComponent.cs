using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.CyberneticImplanter;

[RegisterComponent, NetworkedComponent] //This component is used to give a CyberneticImplanterComponent, the ability to toggle between 2 versions of a cybernetic (left/right)
public sealed partial class CyberneticImplanterModeComponent : Component
{
    /// <summary>
    /// False if left, True if right
    /// </summary>
    [DataField]
    public bool Mode = false;

    /// <summary>
    /// Organ to be implanted when Mode is false
    /// </summary>
    [DataField(required: true)]
    public EntProtoId LeftOrgan;

    /// <summary>
    /// Organ to be implanted when Mode is true
    /// </summary>
    [DataField(required: true)]
    public EntProtoId RightOrgan;

    /// <summary>
    /// Sound played when mode is toggled.
    /// </summary>
    [DataField]
    public SoundSpecifier ModeSwitchSound = new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg", AudioParams.Default.WithVolume(1.5f));
}