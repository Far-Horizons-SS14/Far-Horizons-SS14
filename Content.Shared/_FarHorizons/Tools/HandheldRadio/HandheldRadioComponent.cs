using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Tools.HandheldRadio;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
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

    [DataField]
    public float MicListeningRange = 1.5f;

    [DataField]
    public bool RecievesFromAnyMap = false;
}

[Serializable, NetSerializable]
public sealed class HandheldRadioFrequencyChange(float frequency) : BoundUserInterfaceMessage
{
    public float Frequency { get; } = frequency;
}

[Serializable, NetSerializable]
public sealed class HandheldRadioStateChange(HandheldRadioState state, bool value) : BoundUserInterfaceMessage
{
    public HandheldRadioState State { get; } = state;
    public bool value { get; } = value;
}

public enum HandheldRadioState {
    Microphone,
    Speaker
}

[Serializable, NetSerializable]
public enum HandheldRadioUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum HandheldRadioVisuals
{
    Microphone,
    Speaker
}