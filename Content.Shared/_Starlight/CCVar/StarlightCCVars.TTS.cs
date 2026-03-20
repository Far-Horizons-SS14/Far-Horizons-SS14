using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;
public sealed partial class StarlightCCVars
{
    /// <summary>
    /// URL of the TTS server API.
    /// </summary>
    public static readonly CVarDef<bool> TTSEnabled =
        CVarDef.Create("tts.enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<string> TTSConnectionString =
        CVarDef.Create("tts.connection_string", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
    
    // Far Horizons Start - Create a new CVar to change max length of a message
    /// <summary>
    /// This value defines max length of a message to be pronounced by a TTS system,
    /// messages longer than this will be shortened to this length.
    /// </summary>
    public static readonly CVarDef<int> TTSMaxLengthMessage =
        CVarDef.Create("tts.max_length_message", 50, CVar.ARCHIVE | CVar.SERVER);
    // Far Horizons End
    
    /// <summary>
    /// Option to disable TTS events for client
    /// </summary>
    public static readonly CVarDef<bool> TTSClientEnabled =
        CVarDef.Create("tts.client_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Default volume setting of TTS sound
    /// </summary>
    public static readonly CVarDef<float> TTSVolume =
        CVarDef.Create("tts.volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> TTSRadioVolume =
        CVarDef.Create("tts.radio_volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);
    
    public static readonly CVarDef<bool> TTSRadioQueueEnabled =
        CVarDef.Create("tts.radio_queue_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> TTSAnnounceVolume =
        CVarDef.Create("tts.announce_volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> TTSChimeVolume =
        CVarDef.Create("tts.chime_volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Option to mute radio chime sounds
    /// </summary>
    public static readonly CVarDef<bool> RadioChimeMuted =
        CVarDef.Create("audio.radio_chime_muted", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
