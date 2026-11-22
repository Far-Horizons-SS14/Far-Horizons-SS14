using Robust.Shared.GameStates;
using Robust.Shared.Prototypes; // FH
using Content.Shared.Radio; // FH

namespace Content.Shared.Security.Components;

/// <summary>
/// This is used for storing information about a Genpop ID in order to correctly display it on examine.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class GenpopIdCardComponent : Component
{
    /// <summary>
    /// The crime committed, as a string.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Crime = string.Empty;

    /// <summary>
    /// The length of the sentence
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan SentenceDuration;

    //FH start
    /// <summary>
    /// Channel to annouce expiration on
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Security";
    //FH end
}
