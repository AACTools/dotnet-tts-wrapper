using DotNetTtsWrapper.Events;
using DotNetTtsWrapper.Utils;

namespace DotNetTtsWrapper.Models;

/// <summary>
/// Abstract base class for all TTS clients providing unified interface
/// </summary>
public abstract class AbstractTtsClient : IDisposable
{
    /// <summary>
    /// Currently selected voice ID
    /// </summary>
    protected string? VoiceId { get; set; }

    /// <summary>
    /// Current language (BCP-47 format)
    /// </summary>
    protected string CurrentLanguage = "en-US";

    /// <summary>
    /// TTS properties (rate, pitch, volume)
    /// </summary>
    protected TtsProperties Properties { get; } = new();

    /// <summary>
    /// SSML builder instance
    /// </summary>
    protected SsmlBuilder SsmlBuilder { get; } = new();

    /// <summary>
    /// Audio playback state
    /// </summary>
    protected PlaybackState PlaybackState { get; } = new();

    /// <summary>
    /// Word timings for the current audio
    /// </summary>
    protected List<WordTimingEventArgs> CurrentWordTimings { get; } = new();

    /// <summary>
    /// Audio sample rate in Hz
    /// </summary>
    protected int SampleRate { get; set; } = 24000;

    /// <summary>
    /// Capability flags for this engine
    /// </summary>
    public EngineCapabilities Capabilities { get; protected set; } = new();

    /// <summary>
    /// Get all available voices
    /// </summary>
    public abstract Task<List<TtsVoice>> GetVoicesAsync();

    /// <summary>
    /// Get voices for a specific language
    /// </summary>
    public abstract Task<List<TtsVoice>> GetVoicesByLanguageAsync(string languageCode);

    /// <summary>
    /// Set the voice to use for synthesis
    /// </summary>
    public virtual void SetVoice(string voiceId)
    {
        VoiceId = voiceId ?? throw new ArgumentNullException(nameof(voiceId));
    }

    /// <summary>
    /// Synthesize text to audio bytes (non-streaming)
    /// </summary>
    public abstract Task<TtsSynthesisResult> SynthToBytesAsync(string text, TtsOptions? options = null);

    /// <summary>
    /// Synthesize text with true streaming and word timings
    /// </summary>
    public abstract Task<StreamingTtsResult> SynthToStreamAsync(string text, TtsOptions? options = null);

    /// <summary>
    /// Synthesize and save to file
    /// </summary>
    public abstract Task SynthToFileAsync(string text, string outputPath, AudioFormat format = AudioFormat.Wav, TtsOptions? options = null);

    /// <summary>
    /// Speak text with audio playback
    /// </summary>
    public abstract Task SpeakAsync(string text, TtsOptions? options = null);

    /// <summary>
    /// Speak with streaming playback and word boundary callbacks
    /// </summary>
    public abstract Task SpeakStreamedAsync(string text, Action<WordTimingEventArgs>? wordCallback = null, TtsOptions? options = null);

    /// <summary>
    /// Pause audio playback
    /// </summary>
    public abstract void Pause();

    /// <summary>
    /// Resume audio playback
    /// </summary>
    public abstract void Resume();

    /// <summary>
    /// Stop audio playback
    /// </summary>
    public abstract void Stop();

    /// <summary>
    /// Check if credentials are valid
    /// </summary>
    public abstract Task<CredentialsValidationResult> CheckCredentialsAsync();

    /// <summary>
    /// Set a TTS property
    /// </summary>
    public virtual void SetProperty(string propertyName, object value)
    {
        Properties.SetProperty(propertyName, value);
    }

    /// <summary>
    /// Get a TTS property
    /// </summary>
    public virtual T? GetProperty<T>(string propertyName)
    {
        return Properties.GetProperty<T>(propertyName);
    }

    // Events
    public event EventHandler<WordTimingEventArgs>? WordBoundary;
    public event EventHandler<SpeechStartedEventArgs>? SpeechStarted;
    public event EventHandler<SpeechCompletedEventArgs>? SpeechCompleted;

    /// <summary>
    /// Raise word boundary event
    /// </summary>
    protected virtual void OnWordBoundary(WordTimingEventArgs e)
    {
        WordBoundary?.Invoke(this, e);
        CurrentWordTimings.Add(e);
    }

    /// <summary>
    /// Raise speech started event
    /// </summary>
    protected virtual void OnSpeechStarted(SpeechStartedEventArgs e)
    {
        SpeechStarted?.Invoke(this, e);
    }

    /// <summary>
    /// Raise speech completed event
    /// </summary>
    protected virtual void OnSpeechCompleted(SpeechCompletedEventArgs e)
    {
        SpeechCompleted?.Invoke(this, e);
    }

    /// <summary>
    /// Prepare text for synthesis (handle SSML, Speech Markdown, etc.)
    /// </summary>
    protected async Task<string> PrepareTextAsync(string text, TtsOptions? options)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty", nameof(text));

        // Check if text is already SSML
        bool isSsml = text.TrimStart().StartsWith("<speak", StringComparison.OrdinalIgnoreCase);
        options ??= new TtsOptions();

        if (options.RawSsml || (isSsml && !options.UseSsml))
        {
            return text; // Pass through as-is
        }

        // TODO: Add Speech Markdown conversion here in the future
        // For now, return text as-is or wrap in SSML if needed
        return text;
    }

    /// <summary>
    /// Create SSML from plain text with current properties
    /// </summary>
    protected string CreateSsml(string text, TtsOptions? options = null)
    {
        var builder = SsmlBuilder.Speak();
        builder.Voice(VoiceId ?? "default");

        // Apply properties
        if (options?.Rate != null || Properties.Rate != null)
        {
            var rate = options?.Rate ?? Properties.Rate ?? SpeechRate.Medium;
            builder = builder.WithRate(rate.ToString().ToLowerInvariant());
        }

        if (options?.Pitch != null || Properties.Pitch != null)
        {
            var pitch = options?.Pitch ?? Properties.Pitch ?? SpeechPitch.Medium;
            builder = builder.WithPitch(pitch.ToString().ToLowerInvariant());
        }

        if (options?.Volume != null || Properties.Volume != null)
        {
            var volume = options?.Volume ?? Properties.Volume ?? 100;
            builder = builder.WithVolume(volume);
        }

        builder.AddText(text);
        return builder.Build();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stop();
        }
    }
}

/// <summary>
/// TTS properties container
/// </summary>
public class TtsProperties
{
    private readonly Dictionary<string, object> _properties = new();

    public SpeechRate? Rate { get; set; }
    public SpeechPitch? Pitch { get; set; }
    public int? Volume { get; set; }

    public void SetProperty(string name, object value)
    {
        _properties[name] = value;

        // Update standard properties
        switch (name.ToLowerInvariant())
        {
            case "rate":
                Rate = (SpeechRate)Enum.Parse(typeof(SpeechRate), value.ToString()!);
                break;
            case "pitch":
                Pitch = (SpeechPitch)Enum.Parse(typeof(SpeechPitch), value.ToString()!);
                break;
            case "volume":
                Volume = (int)value;
                break;
        }
    }

    public T? GetProperty<T>(string name)
    {
        if (_properties.TryGetValue(name, out var value))
        {
            return (T)value;
        }
        return default;
    }
}

/// <summary>
/// Audio playback state
/// </summary>
public class PlaybackState
{
    public bool IsPlaying { get; set; }
    public bool IsPaused { get; set; }
    public double CurrentPosition { get; set; }
    public double Duration { get; set; }
}

/// <summary>
/// Engine capabilities flags
/// </summary>
public class EngineCapabilities
{
    public bool SupportsStreaming { get; set; }
    public bool SupportsWordTimings { get; set; }
    public bool SupportsSsml { get; set; }
    public bool SupportsSpeechMarkdown { get; set; }
    public bool RequiresInternet { get; set; }
    public bool IsBrowserSupported { get; set; }
    public bool IsNodeSupported { get; set; }
    public bool IsWindowsSupported { get; set; } = true;
    public bool IsLinuxSupported { get; set; }
    public bool IsMacOsSupported { get; set; }
}