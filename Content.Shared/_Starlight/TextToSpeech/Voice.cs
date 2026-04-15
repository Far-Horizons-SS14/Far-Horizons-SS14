// Far Horizons edit start
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Content.Shared.Humanoid;
// Far Horizons edit end
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.TextToSpeech;
/// <summary>
/// Prototype represent TTS voices
/// </summary>
[Prototype]
public sealed partial class VoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("voice")]
    public int Voice { get; private set; }

    [DataField("name")]
    public string Name { get; private set; } = string.Empty;

    // Far Horizons edit start
    // [DataField("sex", required: true)]
    // public Sex Sex { get; private set; } = default!;
    
    // [DataField("silicon")]
    // public bool Silicon { get; private set; } = false;

    // [DataField]
    // public string? Copyright { get; private set; }

    // [DataField]  
    // public string? License { get; private set; }
    
    [DataField("defaultPitch")]
    public int DefaultPitch { get; private set; } = 0;
    
    [DataField("defaultSpeed")]
    public float DefaultSpeed { get; private set; } = 0.44f;
    
    [DataField("defaultPause")]
    public float DefaultPause { get; private set; } = 0.36f;
    
    [DataField("defaultPolyphony")]
    public int DefaultPolyphony { get; private set; } = 1;
    
    [DataField("defaultScale")]
    public int DefaultScale { get; private set; } = 0;
    
    [DataField("defaultVolume")]
    public float DefaultVolume { get; private set; } = 1.0f;

    [DataField("accessibleForPlayers")] 
    public bool AccessibleForPlayers { get; private set; } = true;
    // Far Horizons edit end
}
