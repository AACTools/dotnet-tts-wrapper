namespace DotNetTtsWrapper.Models;

/// <summary>
/// Represents a TTS voice with unified properties across providers
/// </summary>
public class TtsVoice
{
    /// <summary>
    /// Unique identifier for the voice
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the voice
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gender of the voice
    /// </summary>
    public VoiceGender Gender { get; set; } = VoiceGender.Unknown;

    /// <summary>
    /// Age category of the voice
    /// </summary>
    public VoiceAge Age { get; set; } = VoiceAge.Unknown;

    /// <summary>
    /// TTS provider name
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Language codes supported by this voice
    /// </summary>
    public List<LanguageInfo> LanguageCodes { get; set; } = new();

    /// <summary>
    /// Description of the voice
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Additional metadata specific to the provider
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Natural sample rate in Hz (for voices that have a specific native rate)
    /// </summary>
    public int? NaturalSampleRate { get; set; }
}

/// <summary>
/// Voice gender enumeration
/// </summary>
public enum VoiceGender
{
    Male,
    Female,
    Unknown,
    NonBinary
}

/// <summary>
/// Voice age category
/// </summary>
public enum VoiceAge
{
    Unknown,
    Adult,
    Child,
    Senior,
    Teen
}

/// <summary>
/// Language information
/// </summary>
public class LanguageInfo
{
    /// <summary>
    /// BCP 47 language code (e.g., en-US)
    /// </summary>
    public string Bcp47 { get; set; } = string.Empty;

    /// <summary>
    /// ISO 639-3 language code (e.g., eng)
    /// </summary>
    public string Iso639_3 { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the language
    /// </summary>
    public string Display { get; set; } = string.Empty;
}