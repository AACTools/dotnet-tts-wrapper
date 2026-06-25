using System.Net.Http.Json;
using System.Text.Json;
using DotNetTtsWrapper.Events;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// Google Cloud Text-to-Speech Client
/// Uses Google Cloud TTS REST API
/// </summary>
public class GoogleTtsClient : HttpTtsClientBase
{
    private readonly GoogleCredentials _credentials;
    private const string GoogleTtsUrl = "https://texttospeech.googleapis.com/v1";

    protected override string BaseEndpoint => GoogleTtsUrl;
    protected override bool SupportsStreaming => false;

    public GoogleTtsClient(GoogleCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));

        if (!string.IsNullOrEmpty(_credentials.ApiKey))
        {
            SetApiKeyHeader("x-goog-api-key", _credentials.ApiKey);
        }

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = false,
            SupportsWordTimings = true,
            SupportsSsml = true,
            SupportsSpeechMarkdown = true,
            RequiresInternet = true,
            IsWindowsSupported = true,
            IsLinuxSupported = true,
            IsMacOsSupported = true
        };

        VoiceId = "en-US-Wavenet-D";
    }

    public override string GetSpeechMarkdownPlatform()
    {
        return global::SpeechMarkdown.Platform.GoogleAssistant;
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        // Build the base payload
        var inputObj = new Dictionary<string, object>();
        if (options?.RawSsml == true || text.TrimStart().StartsWith("<speak"))
        {
            inputObj["ssml"] = text;
        }
        else
        {
            inputObj["text"] = text;
        }

        var voiceName = options?.VoiceId ?? VoiceId ?? "en-US-Wavenet-D";
        var languageCode = DeriveLanguageCode(voiceName);

        var voiceObj = new Dictionary<string, object>
        {
            ["languageCode"] = languageCode,
            ["name"] = voiceName
        };

        var audioConfigObj = new Dictionary<string, object>
        {
            ["audioEncoding"] = options?.Format switch
            {
                AudioFormat.Mp3 => "MP3",
                AudioFormat.Wav => "LINEAR16",
                AudioFormat.Ogg => "OGG_OPUS",
                _ => "MP3"
            },
            ["speakingRate"] = GetSpeakingRate(options),
            ["pitch"] = GetPitch(options),
            ["sampleRateHertz"] = 24000
        };

        // Enable timepoints for word boundary events
        if (options?.EnableWordTimings == true)
        {
            audioConfigObj["enableTimepointing"] = true;
        }

        return new
        {
            input = inputObj,
            voice = voiceObj,
            audioConfig = audioConfigObj
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options)
    {
        return "text:synthesize";
    }

    private static string DeriveLanguageCode(string voiceName)
    {
        if (string.IsNullOrEmpty(voiceName) || voiceName.Length < 5)
            return "en-US";
        var parts = voiceName.Split('-');
        if (parts.Length >= 2)
            return $"{parts[0]}-{parts[1]}";
        return "en-US";
    }

    /// <summary>
    /// Process Google TTS timepoints into word timings
    /// </summary>
    private List<WordTimingEventArgs> ProcessTimepoints(JsonElement timepoints)
    {
        var wordTimings = new List<WordTimingEventArgs>();

        try
        {
            if (timepoints.ValueKind != JsonValueKind.Array)
                return wordTimings;

            var timepointArray = timepoints.EnumerateArray().ToList();

            for (int i = 0; i < timepointArray.Count; i++)
            {
                var timepoint = timepointArray[i];

                if (!timepoint.TryGetProperty("markName", out var markName) ||
                    !timepoint.TryGetProperty("timeSeconds", out var timeSeconds))
                    continue;

                var wordText = markName.GetString();
                var startTime = timeSeconds.GetDouble();

                // Calculate end time (next timepoint's start time)
                double endTime = startTime;
                if (i < timepointArray.Count - 1)
                {
                    var nextTimepoint = timepointArray[i + 1];
                    if (nextTimepoint.TryGetProperty("timeSeconds", out var nextTimeSeconds))
                    {
                        endTime = nextTimeSeconds.GetDouble();
                    }
                }

                var timing = new WordTimingEventArgs(wordText ?? "", startTime, endTime);
                wordTimings.Add(timing);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing Google timepoints: {ex.Message}");
        }

        return wordTimings;
    }

    /// <summary>
    /// Override synthesis to process timepoints when word timings are enabled
    /// </summary>
    public override async Task<TtsSynthesisResult> SynthToBytesAsync(string text, TtsOptions? options = null)
    {
        if (options?.EnableWordTimings == true)
        {
            var payload = await BuildSynthesisPayload(text, options);
            var content = JsonContent.Create(payload);
            var response = await _httpClient.PostAsync($"{BaseEndpoint}/{GetSynthesisEndpoint(options)}", content);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonString);

            var audioBase64 = jsonDoc.RootElement.GetProperty("audioContent").GetString();
            var audioBytes = Convert.FromBase64String(audioBase64 ?? "");

            // Process timepoints into word timings
            var wordTimings = new List<WordTimingEventArgs>();
            if (jsonDoc.RootElement.TryGetProperty("timepoints", out var timepoints))
            {
                wordTimings = ProcessTimepoints(timepoints);
            }

            return new TtsSynthesisResult
            {
                AudioData = audioBytes,
                WordTimings = wordTimings,
                Format = options?.Format ?? AudioFormat.Mp3,
                SampleRate = 24000,
                Channels = 1
            };
        }

        return await base.SynthToBytesAsync(text, options);
    }

    /// <summary>
    /// Override streaming synthesis to process timepoints when word timings are enabled
    /// </summary>
    public override async Task<StreamingTtsResult> SynthToStreamAsync(string text, TtsOptions? options = null)
    {
        if (options?.EnableWordTimings == true)
        {
            var bytesResult = await SynthToBytesAsync(text, options);

            return new StreamingTtsResult
            {
                AudioStream = CreateAsyncEnumerableFromBytes(bytesResult.AudioData, bytesResult.Format),
                WordTimings = bytesResult.WordTimings,
                Format = bytesResult.Format,
                SampleRate = bytesResult.SampleRate,
                Channels = bytesResult.Channels,
                FinalAudioData = bytesResult.AudioData
            };
        }

        return await base.SynthToStreamAsync(text, options);
    }

    protected override string GetStreamingEndpoint(TtsOptions options)
    {
        // Google doesn't support true streaming in the REST API
        return "text:synthesize";
    }

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseEndpoint}/voices");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonString);

            var voices = new List<TtsVoice>();
            foreach (var voice in jsonDoc.RootElement.GetProperty("voices").EnumerateArray())
            {
                var voiceIds = voice.GetProperty("name").GetString() ?? "";
                var languageCodes = voice.GetProperty("languageCodes").EnumerateArray().Select(l => l.GetString() ?? "").ToList();

                voices.Add(new TtsVoice
                {
                    Id = voiceIds,
                    Name = voice.GetProperty("name").GetString() ?? "",
                    Gender = MapGender(voice.GetProperty("ssmlGender").GetString()),
                    Provider = "google",
                    LanguageCodes = languageCodes.Select(code => new LanguageInfo
                    {
                        Bcp47 = code,
                        Iso639_3 = code.Split('-')[0],
                        Display = code
                    }).ToList(),
                    NaturalSampleRate = voice.GetProperty("naturalSampleRateHertz").GetInt32()
                });
            }

            return voices;
        }
        catch (Exception)
        {
            return GetFallbackVoices();
        }
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            var voices = await GetVoicesAsync();
            return new CredentialsValidationResult
            {
                IsValid = voices.Count > 0,
                AvailableVoiceCount = voices.Count,
                EngineName = "google"
            };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                EngineName = "google"
            };
        }
    }

    private List<TtsVoice> GetFallbackVoices()
    {
        return new List<TtsVoice>
        {
            new() { Id = "en-US-Wavenet-D", Name = "Wavenet D", Gender = VoiceGender.Male, Provider = "google", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "en-US-Wavenet-A", Name = "Wavenet A", Gender = VoiceGender.Female, Provider = "google", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        };
    }

    private static VoiceGender MapGender(string? gender)
    {
        return gender?.ToLowerInvariant() switch
        {
            "male" => VoiceGender.Male,
            "female" => VoiceGender.Female,
            "neutral" => VoiceGender.Unknown,
            _ => VoiceGender.Unknown
        };
    }

    private double GetSpeakingRate(TtsOptions? options)
    {
        var rate = options?.Rate ?? Properties.Rate ?? SpeechRate.Medium;
        return rate switch
        {
            SpeechRate.XSlow => 0.25,
            SpeechRate.Slow => 0.75,
            SpeechRate.Medium => 1.0,
            SpeechRate.Fast => 1.25,
            SpeechRate.XFast => 1.5,
            _ => 1.0
        };
    }

    private double GetPitch(TtsOptions? options)
    {
        var pitch = options?.Pitch ?? Properties.Pitch ?? SpeechPitch.Medium;
        return pitch switch
        {
            SpeechPitch.XLow => -20.0,
            SpeechPitch.Low => -10.0,
            SpeechPitch.Medium => 0.0,
            SpeechPitch.High => 10.0,
            SpeechPitch.XHigh => 20.0,
            _ => 0.0
        };
    }
}