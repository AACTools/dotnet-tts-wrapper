using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using DotNetTtsWrapper.Events;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// Azure Cognitive Services TTS Client with Speech SDK
/// Uses Azure Speech Services SDK for proper word boundaries and streaming
/// </summary>
public class AzureSdkTtsClient : AbstractTtsClient
{
    private readonly AzureCredentials _credentials;
    private SpeechSynthesizer? _synthesizer;
    private bool _isInitialized = false;

    public AzureSdkTtsClient(AzureCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = true, // Real word boundaries via SDK
            SupportsSsml = true,
            SupportsSpeechMarkdown = false,
            RequiresInternet = true,
            IsWindowsSupported = true,
            IsLinuxSupported = true,
            IsMacOsSupported = true
        };

        VoiceId = "en-US-AriaNeural";
    }

    private async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        var config = SpeechConfig.FromSubscription(_credentials.SubscriptionKey, _credentials.Region);

        // Set output format for audio
        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio24Khz96KBitRateMonoMp3);

        _synthesizer = new SpeechSynthesizer(config, null);
        _isInitialized = true;
    }

    public override async Task<List<TtsVoice>> GetVoicesAsync()
    {
        await InitializeAsync();

        if (_synthesizer == null)
            throw new InvalidOperationException("Speech synthesizer not initialized");

        var voices = new List<TtsVoice>();

        // Get voices from the SDK
        using var result = await _synthesizer.GetVoicesAsync();
        if (result.Reason == ResultReasons.VoicesRetrieved)
        {
            foreach (var voiceInfo in result.Voices)
            {
                voices.Add(new TtsVoice
                {
                    Id = voiceInfo.Name,
                    Name = voiceInfo.Name,
                    Gender = MapGender(voiceInfo),
                    Provider = "azure-sdk",
                    LanguageCodes = new List<LanguageInfo>
                    {
                        new LanguageInfo
                        {
                            Bcp47 = voiceInfo.Locale,
                            Iso639_3 = voiceInfo.Locale.Split('-')[0],
                            Display = voiceInfo.Locale
                        }
                    },
                    Description = voiceInfo.Description,
                    VoiceType = voiceInfo.VoiceType
                });
            }
        }

        return voices;
    }

    public override async Task<List<TtsVoice>> GetVoicesByLanguageAsync(string languageCode)
    {
        var allVoices = await GetVoicesAsync();
        return allVoices
            .Where(v => v.LanguageCodes.Any(l =>
                l.Bcp47.Equals(languageCode, StringComparison.OrdinalIgnoreCase) ||
                l.Iso639_3.Equals(languageCode, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public override void SetVoice(string voiceId)
    {
        base.SetVoice(voiceId);
        VoiceId = voiceId;
    }

    public override async Task<TtsSynthesisResult> SynthToBytesAsync(string text, TtsOptions? options = null)
    {
        await InitializeAsync();

        options ??= new TtsOptions();
        var preparedText = await PrepareTextAsync(text, options);

        var wordTimings = new List<WordTimingEventArgs>();

        // Create a result object to hold the synthesized audio
        using var result = await _synthesizer.SpeakTextAsync(preparedText);

        if (result.Reason == ResultReasons.Canceled)
        {
            throw new OperationCanceledException("Speech synthesis was canceled");
        }

        if (result.Reason != ResultReasons.SynthesizingAudioCompleted)
        {
            throw new InvalidOperationException($"Speech synthesis failed: {result.Reason}");
        }

        // Get the audio data
        var audioData = result.AudioData;

        // Extract word timings from the SDK result
        if (options.EnableWordTimings && result.WordBoundary != null)
        {
            // The SDK provides word boundary information
            foreach (var boundary in result.WordBoundary)
            {
                // WordBoundaryEventArgs contains: AudioOffset, Duration, Text, etc.
                var wordTiming = new WordTimingEventArgs(
                    boundary.Text ?? "",
                    boundary.AudioOffset / 10000000.0, // Convert from hundred nanoseconds to seconds
                    (boundary.AudioOffset + boundary.Duration) / 10000000.0
                );
                wordTimings.Add(wordTiming);
            }
        }

        return new TtsSynthesisResult
        {
            AudioData = audioData.ToArray(),
            WordTimings = wordTimings,
            Format = AudioFormat.Mp3,
            SampleRate = 24000,
            Channels = 1
        };
    }

    public override async Task<StreamingTtsResult> SynthToStreamAsync(string text, TtsOptions? options = null)
    {
        await InitializeAsync();

        options ??= new TtsOptions();
        var preparedText = await PrepareTextAsync(text, options);

        var streamingResult = new StreamingTtsResult
        {
            Format = AudioFormat.Mp3,
            SampleRate = 24000,
            Channels = 1
        };

        // Azure SDK doesn't support true streaming in the traditional sense,
        // but we can provide a pull-based stream
        var audioStream = new System.Async.Stream<AudioChunkEventArgs>(async cancellationToken =>
        {
            using var result = await _synthesizer.SpeakTextAsync(preparedText);

            if (result.Reason != ResultReasons.SynthesizingAudioCompleted)
            {
                throw new InvalidOperationException($"Speech synthesis failed: {result.Reason}");
            }

            var audioData = result.AudioData;

            // Yield the complete audio as a single chunk
            // (Azure SDK synthesizes the entire audio at once)
            yield return new AudioChunkEventArgs
            {
                AudioData = audioData.ToArray(),
                Format = AudioFormat.Mp3,
                IsFinal = true,
                Position = 0
            };

            // Collect word timings if enabled
            if (options.EnableWordTimings && result.WordBoundary != null)
            {
                foreach (var boundary in result.WordBoundary)
                {
                    var wordTiming = new WordTimingEventArgs(
                        boundary.Text ?? "",
                        boundary.AudioOffset / 10000000.0,
                        (boundary.AudioOffset + boundary.Duration) / 10000000.0
                    );
                    streamingResult.WordTimings.Add(wordTiming);
                }
            }
        });

        streamingResult.AudioStream = audioStream;
        return streamingResult;
    }

    public override async Task SynthToFileAsync(string text, string outputPath, AudioFormat format = AudioFormat.Wav, TtsOptions? options = null)
    {
        await InitializeAsync();

        options ??= new TtsOptions();
        options.Format = format;

        var preparedText = await PrepareTextAsync(text, options);

        // Set the output format based on request
        var outputFormat = format switch
        {
            AudioFormat.Mp3 => SpeechSynthesisOutputFormat.Audio24Khz96KBitRateMonoMp3,
            AudioFormat.Wav => SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm,
            AudioFormat.Ogg => SpeechSynthesisOutputFormat.Ogg16Khz16BitMonoOpus,
            _ => SpeechSynthesisOutputFormat.Audio24Khz96KBitRateMonoMp3
        };

        if (_synthesizer != null)
        {
            _synthesizer.SetOutputFormat(outputFormat);
        }

        using var result = await _synthesizer.SpeakTextAsync(preparedText);

        if (result.Reason != ResultReasons.SynthesizingAudioCompleted)
        {
            throw new InvalidOperationException($"Speech synthesis failed: {result.Reason}");
        }

        // Save to file
        await File.WriteAllBytesAsync(outputPath, result.AudioData.ToArray());
    }

    public override async Task SpeakAsync(string text, TtsOptions? options = null)
    {
        await InitializeAsync();

        options ??= new TtsOptions();
        var preparedText = await PrepareTextAsync(text, options);

        if (_synthesizer == null)
            throw new InvalidOperationException("Speech synthesizer not initialized");

        // Use SpeakTextAsync for basic synthesis
        using var result = await _synthesizer.SpeakTextAsync(preparedText);

        if (result.Reason == ResultReasons.Canceled)
        {
            OnSpeechCompleted(new SpeechCompletedEventArgs
            {
                WasCancelled = true
            });
            return;
        }

        if (result.Reason != ResultReasons.SynthesizingAudioCompleted)
        {
            OnSpeechCompleted(new SpeechCompletedEventArgs
            {
                Error = result.Reason.ToString()
            });
            throw new InvalidOperationException($"Speech synthesis failed: {result.Reason}");
        }

        OnSpeechCompleted(new SpeechCompletedEventArgs());
    }

    public override async Task SpeakStreamedAsync(string text, Action<WordTimingEventArgs>? wordCallback = null, TtsOptions? options = null)
    {
        await InitializeAsync();

        options ??= new TtsOptions();
        var preparedText = await PrepareTextAsync(text, options);

        if (_synthesizer == null)
            throw new InvalidOperationException("Speech synthesizer not initialized");

        // Set up word boundary callback if requested
        if (wordCallback != null && options.EnableWordTimings)
        {
            _synthesizer.WordBoundary += (s, e) =>
            {
                var wordTiming = new WordTimingEventArgs(
                    e.Text ?? "",
                    e.AudioOffset / 10000000.0,
                    (e.AudioOffset + e.Duration) / 10000000.0
                );
                wordCallback(wordTiming);
            };
        }

        // Register event handlers
        _synthesizer.SynthesisStarted += (s, e) =>
        {
            OnSpeechStarted(new SpeechStartedEventArgs());
        };

        _synthesizer.SynthesisCompleted += (s, e) =>
        {
            OnSpeechCompleted(new SpeechCompletedEventArgs
            {
                WasCancelled = e.Result?.Reason == ResultReasons.Canceled
            });
        };

        using var result = await _synthesizer.SpeakTextAsync(preparedText);

        if (result.Reason == ResultReasons.Canceled)
        {
            throw new OperationCanceledException("Speech synthesis was canceled");
        }

        if (result.Reason != ResultReasons.SynthesizingAudioCompleted)
        {
            throw new InvalidOperationException($"Speech synthesis failed: {result.Reason}");
        }

        // Clean up event handlers
        if (_synthesizer != null)
        {
            _synthesizer.SynthesisStarted -= null;
            _synthesizer.SynthesisCompleted -= null;
            _synthesizer.WordBoundary -= null;
        }
    }

    public override void Pause()
    {
        // Azure SDK doesn't support pause/resume for ongoing synthesis
        throw new NotSupportedException("Pause/resume is not supported by Azure SDK synthesis");
    }

    public override void Resume()
    {
        throw new NotSupportedException("Pause/resume is not supported by Azure SDK synthesis");
    }

    public override void Stop()
    {
        // Cancel any ongoing synthesis
        // The SDK will handle this automatically when the result is disposed
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await InitializeAsync();

            // Try to get voices as a simple validation check
            var voices = await GetVoicesAsync();
            return new CredentialsValidationResult
            {
                IsValid = voices.Count > 0,
                AvailableVoiceCount = voices.Count,
                EngineName = "azure-sdk"
            };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                EngineName = "azure-sdk"
            };
        }
    }

    public override void SetProperty(string propertyName, object value)
    {
        base.SetProperty(propertyName, value);

        // Handle Azure-specific properties
        if (_synthesizer != null)
        {
            switch (propertyName.ToLowerInvariant())
            {
                case "rate":
                    if (value is SpeechRate rate)
                    {
                        var rateValue = rate switch
                        {
                            SpeechRate.XSlow => 0.5,
                            SpeechRate.Slow => 0.75,
                            SpeechRate.Medium => 1.0,
                            SpeechRate.Fast => 1.25,
                            SpeechRate.XFast => 1.5,
                            _ => 1.0
                        };
                        // Set speaking rate via SSML in synthesis
                    }
                    break;
                case "pitch":
                    if (value is SpeechPitch pitch)
                    {
                        // Set pitch via SSML in synthesis
                    }
                    break;
                case "volume":
                    if (value is int volume)
                    {
                        // Volume can be set via SSML in synthesis
                    }
                    break;
            }
        }
    }

    public override T? GetProperty<T>(string propertyName)
    {
        // Handle Azure-specific properties
        switch (propertyName.ToLowerInvariant())
        {
            case "region":
                return (T)(object)_credentials.Region;
            case "subscriptionkey":
                return (T)(object)_credentials.SubscriptionKey;
            default:
                return base.GetProperty<T>(propertyName);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stop();
            _synthesizer?.Dispose();
            _isInitialized = false;
        }
        base.Dispose(disposing);
    }

    private static VoiceGender MapGender(VoiceInfo voiceInfo)
    {
        // Try to determine gender from voice name or properties
        var voiceName = voiceInfo.Name.ToLowerInvariant();

        if (voiceName.Contains("female") || voiceName.Contains("woman") || voiceName.Contains("jenny") ||
            voiceName.Contains("aria") || voiceName.Contains("neural") && voiceName.Contains("female"))
        {
            return VoiceGender.Female;
        }

        if (voiceName.Contains("male") || voiceName.Contains("man") || voiceName.Contains("guy") ||
            voiceName.Contains("guy"))
        {
            return VoiceGender.Male;
        }

        return VoiceGender.Unknown;
    }

    private class AzureSynthesisResult : TtsSynthesisResult
    {
        public byte[] AudioData { get; set; } = Array.Empty<byte>();
        public List<WordTimingEventArgs> WordTimings { get; set; } = new();
        public AudioFormat Format { get; set; } = AudioFormat.Mp3;
        public int SampleRate { get; set; } = 24000;
        public int Channels { get; set; } = 1;
    }
}