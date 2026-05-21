using System.Net.Http.Json;
using System.Text.Json;
using DotNetTtsWrapper.Events;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// ElevenLabs TTS Client
/// Uses ElevenLabs API for high-quality text-to-speech
/// </summary>
public class ElevenLabsTtsClient : HttpTtsClientBase
{
    private readonly ElevenLabsCredentials _credentials;
    private const string ElevenLabsApiUrl = "https://api.elevenlabs.io/v1";

    protected override string BaseEndpoint => ElevenLabsApiUrl;
    protected override bool SupportsStreaming => true;

    public ElevenLabsTtsClient(ElevenLabsCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("xi-api-key", _credentials.ApiKey);

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = true, // ElevenLabs supports word timings
            SupportsSsml = false, // ElevenLabs doesn't support standard SSML
            SupportsSpeechMarkdown = false,
            RequiresInternet = true,
            IsWindowsSupported = true,
            IsLinuxSupported = true,
            IsMacOsSupported = true
        };

        VoiceId = "21m00Tcm4TlvDq8ikWAM"; // Default voice (Rachel)
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            text = text,
            model_id = "eleven_multilingual_v2", // or eleven_monolingual_v1
            voice_settings = new
            {
                stability = 0.5,
                similarity_boost = 0.75
            }
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options)
    {
        var voiceId = options.VoiceId ?? VoiceId ?? "21m00Tcm4TlvDq8ikWAM";
        return $"text-to-speech/{voiceId}";
    }

    protected override string GetStreamingEndpoint(TtsOptions options)
    {
        var voiceId = options.VoiceId ?? VoiceId ?? "21m00Tcm4TlvDq8ikWAM";
        return $"text-to-speech/{voiceId}/stream";
    }

    // Special method to get word timings from ElevenLabs
    public async Task<StreamingTtsResult> SynthToStreamWithTimingsAsync(string text, TtsOptions? options = null)
    {
        options ??= new TtsOptions();
        var preparedText = await PrepareTextAsync(text, options);

        var voiceId = options.VoiceId ?? VoiceId ?? "21m00Tcm4TlvDq8ikWAM";

        // ElevenLabs supports word timings via a different endpoint
        var payload = await BuildSynthesisPayload(preparedText, options);
        var fullUrl = $"{BaseEndpoint.TrimEnd('/')}/text-to-speech/{voiceId}?enable_timestamps=true";

        var content = JsonContent.Create(payload);
        var response = await _httpClient.PostAsync(fullUrl, content);
        response.EnsureSuccessStatusCode();

        // ElevenLabs returns JSON with audio and word timings
        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(jsonString);

        var audioBase64 = jsonDoc.RootElement.GetProperty("audio_base64").GetString();
        var audioBytes = Convert.FromBase64String(audioBase64 ?? "");

        // Parse word timings if available
        var wordTimings = new List<WordTimingEventArgs>();
        if (jsonDoc.RootElement.TryGetProperty("alignment", out var alignment))
        {
            foreach (var word in alignment.EnumerateObject())
            {
                var wordText = word.Name;
                var timings = word.Value;

                if (timings.TryGetProperty("start", out var startElem) &&
                    timings.TryGetProperty("end", out var endElem))
                {
                    var startTime = startElem.GetDouble() / 10000.0; // Convert from hundred nanoseconds
                    var endTime = endElem.GetDouble() / 10000.0;

                    var timing = new WordTimingEventArgs(wordText, startTime, endTime);
                    wordTimings.Add(timing);
                }
            }
        }

        return new StreamingTtsResult
        {
            AudioStream = CreateAsyncEnumerableFromBytes(audioBytes, options.Format),
            WordTimings = wordTimings,
            Format = options.Format,
            SampleRate = 44100, // ElevenLabs uses 44.1kHz
            Channels = 1,
            FinalAudioData = audioBytes
        };
    }

    public override async Task<StreamingTtsResult> SynthToStreamAsync(string text, TtsOptions? options = null)
    {
        if (options?.EnableWordTimings == true)
        {
            return await SynthToStreamWithTimingsAsync(text, options);
        }

        return await base.SynthToStreamAsync(text, options);
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
            foreach (var voice in jsonDoc.RootElement.EnumerateArray())
            {
                var voiceId = voice.GetProperty("voice_id").GetString();
                var voiceName = voice.GetProperty("name").GetString();

                var labels = voice.GetProperty("labels");
                var gender = "Unknown";
                if (labels.TryGetProperty("gender", out var genderElem))
                {
                    gender = genderElem.GetString() ?? "Unknown";
                }

                var languageCodes = new List<LanguageInfo>();
                if (labels.TryGetProperty("accent", out var accentElem))
                {
                    var accent = accentElem.GetString();
                    if (!string.IsNullOrEmpty(accent))
                    {
                        languageCodes.Add(new LanguageInfo
                        {
                            Bcp47 = accent, // ElevenLabs uses accent codes
                            Iso639_3 = accent.Substring(0, 2),
                            Display = accent
                        });
                    }
                }

                voices.Add(new TtsVoice
                {
                    Id = voiceId ?? "",
                    Name = voiceName ?? "",
                    Gender = MapGender(gender),
                    Provider = "elevenlabs",
                    LanguageCodes = languageCodes
                });
            }

            return voices;
        }
        catch (Exception)
        {
            // Fallback to common voices if API call fails
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
                EngineName = "elevenlabs"
            };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                EngineName = "elevenlabs"
            };
        }
    }

    private List<TtsVoice> GetFallbackVoices()
    {
        return new List<TtsVoice>
        {
            new() { Id = "21m00Tcm4TlvDq8ikWAM", Name = "Rachel", Gender = VoiceGender.Female, Provider = "elevenlabs" },
            new() { Id = "AZnzlk1XvdvUeBnXmlld", Name = "Domi", Gender = VoiceGender.Female, Provider = "elevenlabs" },
            new() { Id = "EXAVITQu4vr4xnSDxMaL", Name = "Bella", Gender = VoiceGender.Female, Provider = "elevenlabs" },
            new() { Id = "ErXwobaYi9GEDJNZH8qj", Name = "Antoni", Gender = VoiceGender.Male, Provider = "elevenlabs" },
            new() { Id = "MF3mGyEYCl7XYWbV9V6O", Name = "Elli", Gender = VoiceGender.Female, Provider = "elevenlabs" }
        };
    }

    private static VoiceGender MapGender(string gender)
    {
        return gender.ToLowerInvariant() switch
        {
            "male" => VoiceGender.Male,
            "female" => VoiceGender.Female,
            _ => VoiceGender.Unknown
        };
    }
}