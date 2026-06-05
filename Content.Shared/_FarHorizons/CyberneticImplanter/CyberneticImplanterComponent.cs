using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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

    /// <summary>
    /// Organ to be implanted
    /// </summary>
    [DataField]
    public ProtoId<EntityPrototype>? ImplantedOrgan;

    /// <summary>
    /// Time to activate on a target
    /// </summary>
    [DataField]
    public TimeSpan ActivationTime = TimeSpan.FromSeconds(5f);
}

[Serializable, NetSerializable]
public sealed partial class CyberneticImplantDoAfterEvent : SimpleDoAfterEvent
{
}