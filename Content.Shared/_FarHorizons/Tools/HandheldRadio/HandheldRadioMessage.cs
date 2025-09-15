using Robust.Shared.Serialization;

namespace Content.Shared.FarHorizons.Tools.HandheldRadio;

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