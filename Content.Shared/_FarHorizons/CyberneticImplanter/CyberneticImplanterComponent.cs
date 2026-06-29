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
    public SoundSpecifier? ImplantBeginSound;

    /// <summary>
    /// Sound played on Implant end.
    /// </summary>
    [DataField]
    public SoundSpecifier? ImplantEndSound;

    /// <summary>
    /// Organ to be implanted
    /// </summary>
    [DataField(required: true)]
    public EntProtoId ImplantedOrgan;

    /// <summary>
    /// Description of Organ to be implanted, automatically generated upon mapinit if not provided
    /// </summary>
    [DataField]
    public string? ImplantedOrganDesc;

    /// <summary>
    /// Time to activate on a target
    /// </summary>
    [DataField]
    public TimeSpan ActivationTime = TimeSpan.FromSeconds(10f);
}

[Serializable, NetSerializable]
public enum CyberneticImplanterVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum CyberneticImplanterState : byte
{
    Icon,
    Implant,
    Used,
    Gored,
}

[Serializable, NetSerializable]
public sealed partial class CyberneticImplanterDoAfterEvent : SimpleDoAfterEvent;