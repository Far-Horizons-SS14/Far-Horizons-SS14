using System.Text.Json.Serialization;

namespace Content.Server._Starlight.TextToSpeech;

public sealed class TtsJob
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("t")]
    public required string Text { get; set; }

    [JsonPropertyName("r")]
    public required string Voice { get; set; }

    [JsonPropertyName("e")]
    public int Effect { get; set; }

    [JsonPropertyName("v")]
    public int Version { get; set; } = 2;

    [JsonPropertyName("ts")]
    public long Timestamp { get; set; }
    
    // Far Horizons edit start - Symspeech
    [JsonPropertyName("pitch")]
    public int Pitch { get; set; } = 0;
    
    [JsonPropertyName("speed")]
    public float Speed { get; set; } = 0.44f;
    
    [JsonPropertyName("pause")]
    public float Pause { get; set; } = 0.36f;
    
    [JsonPropertyName("poly")]
    public int Polyphony { get; set; } = 1;
    
    [JsonPropertyName("vol")]
    public float Volume { get; set; } = 1.0f;
    // Far Horizons edit end 
}

[JsonSerializable(typeof(TtsJob))]
internal sealed partial class TtsJobContext : JsonSerializerContext
{
}