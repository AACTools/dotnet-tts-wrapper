using System.Net.Http.Json;
using System.Text.Json;
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
            SupportsSpeechMarkdown = false,
            RequiresInternet = true,
            IsWindowsSupported = true,
            IsLinuxSupported = true,
            IsMacOsSupported = true
        };

        VoiceId = "en-US-Wavenet-D";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            input = new
            {
                text = text,
                ssml = options?.RawSsml == true || text.TrimStart().StartsWith("<speak") ? text : null
            },
            voice = new
            {
                languageCode = "en-US",
                name = options?.VoiceId ?? VoiceId ?? "en-US-Wavenet-D"
            },
            audioConfig = new
            {
                audioEncoding = options?.Format switch
                {
                    AudioFormat.Mp3 => "MP3",
                    AudioFormat.Wav => "LINEAR16",
                    AudioFormat.Ogg => "OGG_OPUS",
                    _ => "MP3"
                },
                speakingRate = GetSpeakingRate(options),
                pitch = GetPitch(options),
                sampleRateHertz = 24000
            }
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options)
    {
        return "text:synthesize";
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