namespace DotNetTtsWrapper.Models;

/// <summary>
/// Options for speech synthesis
/// </summary>
public class TtsOptions
{
    /// <summary>
    /// Speech rate
    /// </summary>
    public SpeechRate? Rate { get; set; }

    /// <summary>
    /// Speech pitch
    /// </summary>
    public SpeechPitch? Pitch { get; set; }

    /// <summary>
    /// Speech volume (0-100)
    /// </summary>
    public int? Volume { get; set; }

    /// <summary>
    /// Whether to use word boundary information for streaming synthesis
    /// </summary>
    public bool UseWordBoundary { get; set; } = true;

    /// <summary>
    /// Voice ID to use for synthesis
    /// </summary>
    public string? VoiceId { get; set; }

    /// <summary>
    /// Audio format to use for synthesis
    /// </summary>
    public AudioFormat Format { get; set; } = AudioFormat.Wav;

    /// <summary>
    /// Raw SSML to pass directly to the provider, bypassing Speech Markdown conversion
    /// </summary>
    public bool RawSsml { get; set; } = false;

    /// <summary>
    /// Whether to enable word timing information
    /// </summary>
    public bool EnableWordTimings { get; set; } = true;

    /// <summary>
    /// Whether to use SSML input
    /// </summary>
    public bool UseSsml { get; set; } = false;
}

/// <summary>
/// Speech rate enumeration
/// </summary>
public enum SpeechRate
{
    XSlow,
    Slow,
    Medium,
    Fast,
    XFast
}

/// <summary>
/// Speech pitch enumeration
/// </summary>
public enum SpeechPitch
{
    XLow,
    Low,
    Medium,
    High,
    XHigh
}

/// <summary>
/// Audio format enumeration
/// </summary>
public enum AudioFormat
{
    Wav,
    Mp3,
    Ogg,
    Opus,
    Aac,
    Flac,
    Pcm
}