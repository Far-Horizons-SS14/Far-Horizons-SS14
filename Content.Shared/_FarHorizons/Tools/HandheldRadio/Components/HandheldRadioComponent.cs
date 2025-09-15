using Robust.Shared.GameStates;

namespace Content.Shared.FarHorizons.Tools.HandheldRadio.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HandheldRadioComponent : Component
{
    [DataField]
    public float FrequencyMin = 80.0f;

    [DataField]
    public float FrequencyMax = 140.0f;

    [DataField, AutoNetworkedField]
    public float CurrentFrequency = 88.3f;

    [DataField, AutoNetworkedField]
    public bool MicEnabled = false;

    [DataField, AutoNetworkedField]
    public bool SpeakerEnabled = false;

    [DataField, AutoNetworkedField]
    public float MicListeningRange = 1.5f;

    [DataField, AutoNetworkedField]
    public bool RecievesFromAnyMap = false;
}
