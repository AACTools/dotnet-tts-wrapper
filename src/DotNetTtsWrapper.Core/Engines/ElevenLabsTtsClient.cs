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
            SupportsWordTimings = true,
            SupportsSsml = false,
            SupportsSpeechMarkdown = true,
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
            model_id = _credentials.ModelId,
            voice_settings = new
            {
                stability = _credentials.Stability,
                similarity_boost = _credentials.SimilarityBoost
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

        // ElevenLabs supports word timings via the with-timestamps endpoint
        var payload = await BuildSynthesisPayload(preparedText, options);
        var fullUrl = $"{BaseEndpoint.TrimEnd('/')}/text-to-speech/{voiceId}/with-timestamps";

        var content = JsonContent.Create(payload);
        var response = await _httpClient.PostAsync(fullUrl, content);
        response.EnsureSuccessStatusCode();

        // ElevenLabs returns JSON with audio and character alignment
        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(jsonString);

        var audioBase64 = jsonDoc.RootElement.GetProperty("audio_base64").GetString();
        var audioBytes = Convert.FromBase64String(audioBase64 ?? "");

        // Parse character alignment and convert to word boundaries
        var wordTimings = new List<WordTimingEventArgs>();
        if (jsonDoc.RootElement.TryGetProperty("alignment", out var alignment))
        {
            wordTimings = ConvertCharacterTimingToWordBoundaries(preparedText, alignment);
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

    /// <summary>
    /// Convert ElevenLabs character-level timing to word boundaries
    /// </summary>
    private List<WordTimingEventArgs> ConvertCharacterTimingToWordBoundaries(string text, JsonElement alignment)
    {
        var wordTimings = new List<WordTimingEventArgs>();

        try
        {
            // Extract character timing arrays
            if (!alignment.TryGetProperty("characters", out var charsElem) ||
                !alignment.TryGetProperty("character_start_times_seconds", out var startTimesElem) ||
                !alignment.TryGetProperty("character_end_times_seconds", out var endTimesElem))
            {
                return wordTimings;
            }

            var characters = charsElem.EnumerateArray().Select(e => e.GetString()).ToArray();
            var startTimes = startTimesElem.EnumerateArray().Select(e => e.GetDouble()).ToArray();
            var endTimes = endTimesElem.EnumerateArray().Select(e => e.GetDouble()).ToArray();

            // Split text into words while preserving positions
            var words = new List<(string word, int startIndex, int endIndex)>();
            var wordRegex = new System.Text.RegularExpressions.Regex(@"\S+");
            var matches = wordRegex.Matches(text);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                words.Add((match.Value, match.Index, match.Index + match.Value.Length - 1));
            }

            // Convert each word to boundary data using character timing
            foreach (var (wordText, startIndex, endIndex) in words)
            {
                // Make sure we have timing data for these character positions
                if (startIndex < startTimes.Length && endIndex < endTimes.Length)
                {
                    var startTime = startTimes[startIndex];
                    var endTime = endTimes[endIndex];

                    var timing = new WordTimingEventArgs(wordText, startTime, endTime);
                    wordTimings.Add(timing);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting character timing to word boundaries: {ex.Message}");
        }

        return wordTimings;
    }

    public override async Task<StreamingTtsResult> SynthToStreamAsync(string text, TtsOptions? options = null)
    {
        // ElevenLabs supports word timings via the with-timestamps endpoint
        if (options?.EnableWordTimings == true)
        {
            return await SynthToStreamWithTimingsAsync(text, options);
        }

        return await base.SynthToStreamAsync(text, options);
    }

    public override async Task<TtsSynthesisResult> SynthToBytesAsync(string text, TtsOptions? options = null)
    {
        // ElevenLabs supports word timings via the with-timestamps endpoint
        if (options?.EnableWordTimings == true)
        {
            var streamingResult = await SynthToStreamWithTimingsAsync(text, options);

            return new TtsSynthesisResult
            {
                AudioData = streamingResult.FinalAudioData ?? Array.Empty<byte>(),
                WordTimings = streamingResult.WordTimings,
                Format = streamingResult.Format,
                SampleRate = streamingResult.SampleRate,
                Channels = streamingResult.Channels
            };
        }

        return await base.SynthToBytesAsync(text, options);
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