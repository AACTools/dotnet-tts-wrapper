using DotNetTtsWrapper.Events;
using DotNetTtsWrapper.Utils;
using System.Text.RegularExpressions;

namespace DotNetTtsWrapper.Models;

public abstract class AbstractTtsClient : IDisposable
{
    protected string? VoiceId { get; set; }
    protected string CurrentLanguage = "en-US";
    protected TtsProperties Properties { get; } = new();
    protected SsmlBuilder SsmlBuilder { get; } = new();
    protected PlaybackState PlaybackState { get; } = new();
    protected List<WordTimingEventArgs> CurrentWordTimings { get; } = new();
    protected int SampleRate { get; set; } = 24000;
    public EngineCapabilities Capabilities { get; protected set; } = new();

    private SpeechMarkdownConverter? _speechMarkdownConverter;

    private SpeechMarkdownConverter SpeechMarkdownConverter
    {
        get { return _speechMarkdownConverter ??= new SpeechMarkdownConverter(); }
    }

    public abstract Task<List<TtsVoice>> GetVoicesAsync();
    public abstract Task<List<TtsVoice>> GetVoicesByLanguageAsync(string languageCode);

    public virtual void SetVoice(string voiceId)
    {
        VoiceId = voiceId ?? throw new ArgumentNullException(nameof(voiceId));
    }

    public abstract Task<TtsSynthesisResult> SynthToBytesAsync(string text, TtsOptions? options = null);
    public abstract Task<StreamingTtsResult> SynthToStreamAsync(string text, TtsOptions? options = null);
    public abstract Task SynthToFileAsync(string text, string outputPath, AudioFormat format = AudioFormat.Wav, TtsOptions? options = null);
    public abstract Task SpeakAsync(string text, TtsOptions? options = null);
    public abstract Task SpeakStreamedAsync(string text, Action<WordTimingEventArgs>? wordCallback = null, TtsOptions? options = null);
    public abstract void Pause();
    public abstract void Resume();
    public abstract void Stop();
    public abstract Task<CredentialsValidationResult> CheckCredentialsAsync();

    public virtual void SetProperty(string propertyName, object value)
    {
        Properties.SetProperty(propertyName, value);
    }

    public virtual T? GetProperty<T>(string propertyName)
    {
        return Properties.GetProperty<T>(propertyName);
    }

    public event EventHandler<WordTimingEventArgs>? WordBoundary;
    public event EventHandler<SpeechStartedEventArgs>? SpeechStarted;
    public event EventHandler<SpeechCompletedEventArgs>? SpeechCompleted;

    protected virtual void OnWordBoundary(WordTimingEventArgs e)
    {
        WordBoundary?.Invoke(this, e);
        CurrentWordTimings.Add(e);
    }

    protected virtual void OnSpeechStarted(SpeechStartedEventArgs e)
    {
        SpeechStarted?.Invoke(this, e);
    }

    protected virtual void OnSpeechCompleted(SpeechCompletedEventArgs e)
    {
        SpeechCompleted?.Invoke(this, e);
    }

    protected virtual string GetSpeechMarkdownPlatform()
    {
        return global::SpeechMarkdown.Platform.W3c;
    }

    protected async Task<string> PrepareTextAsync(string text, TtsOptions? options)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty", nameof(text));

        options ??= new TtsOptions();

        bool isSsml = text.TrimStart().StartsWith("<speak", StringComparison.OrdinalIgnoreCase);

        if (options.RawSsml)
        {
            return text;
        }

        if (isSsml)
        {
            return text;
        }

        bool useSpeechMarkdown = NormalizeUseSpeechMarkdown(text, options);

        if (useSpeechMarkdown && SpeechMarkdownConverter.IsSpeechMarkdown(text))
        {
            if (Capabilities.SupportsSsml)
            {
                var platform = GetSpeechMarkdownPlatform();
                var ssml = SpeechMarkdownConverter.ToSsml(text, platform);
                return ssml;
            }
            else
            {
                var plainText = SpeechMarkdownConverter.ToText(text);
                return plainText;
            }
        }

        return text;
    }

    private bool NormalizeUseSpeechMarkdown(string text, TtsOptions options)
    {
        if (options.UseSpeechMarkdown.HasValue)
            return options.UseSpeechMarkdown.Value;

        if (options.RawSsml)
            return false;

        if (text.TrimStart().StartsWith("<speak", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!SpeechMarkdownConverter.IsSpeechMarkdown(text))
            return false;

        return true;
    }

    protected static bool IsSsml(string text)
    {
        return !string.IsNullOrEmpty(text) &&
               text.TrimStart().StartsWith("<speak", StringComparison.OrdinalIgnoreCase);
    }

    protected static string StripSsml(string ssml)
    {
        if (string.IsNullOrEmpty(ssml))
            return ssml;

        var content = ssml.Trim();

        if (content.StartsWith("<speak", StringComparison.OrdinalIgnoreCase))
        {
            var firstGt = content.IndexOf('>');
            if (firstGt >= 0)
                content = content.Substring(firstGt + 1);

            if (content.EndsWith("</speak>", StringComparison.OrdinalIgnoreCase))
                content = content.Substring(0, content.Length - "</speak>".Length);
        }

        return Regex.Replace(content, @"<[^>]+>", "").Trim();
    }

    protected string CreateSsml(string text, TtsOptions? options = null)
    {
        var builder = SsmlBuilder.Create();
        builder.Voice(VoiceId ?? "default");

        var rate = options?.Rate ?? Properties.Rate;
        var pitch = options?.Pitch ?? Properties.Pitch;
        var volume = options?.Volume ?? Properties.Volume;

        if (rate != null || pitch != null || volume != null)
        {
            builder.BeginProsody(rate, pitch, volume);
        }

        builder.AddText(text);

        if (rate != null || pitch != null || volume != null)
        {
            builder.EndProsody();
        }

        builder.EndVoice();
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
            _speechMarkdownConverter?.Dispose();
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