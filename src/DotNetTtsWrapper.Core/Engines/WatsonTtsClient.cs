using System.Net.Http.Json;
using System.Text.Json;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// IBM Watson TTS Client
/// Uses IBM Watson Text to Speech API
/// </summary>
public class WatsonTtsClient : HttpTtsClientBase
{
    private readonly WatsonCredentials _credentials;

    protected override string BaseEndpoint => _credentials.ServiceUrl;
    protected override bool SupportsStreaming => true;

    public WatsonTtsClient(WatsonCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetAuthentication("Basic", $"apikey:{_credentials.ApiKey}");

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = true,
            SupportsSsml = true,
            SupportsSpeechMarkdown = true,
            RequiresInternet = true,
            IsWindowsSupported = true,
            IsLinuxSupported = true,
            IsMacOsSupported = true
        };

        VoiceId = "en-US_MichaelV3Voice";
    }

    public override string GetSpeechMarkdownPlatform()
    {
        return global::SpeechMarkdown.Platform.IbmWatson;
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            text = text,
            voice = options?.VoiceId ?? VoiceId ?? "en-US_MichaelV3Voice",
            accept = options?.Format switch
            {
                AudioFormat.Mp3 => "audio/mp3",
                AudioFormat.Wav => "audio/wav",
                AudioFormat.Ogg => "audio/ogg;codecs=opus",
                _ => "audio/mp3"
            }
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options)
    {
        return "v1/synthesize";
    }

    protected override string GetStreamingEndpoint(TtsOptions options)
    {
        return "v1/synthesize";
    }

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseEndpoint}/v1/voices");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonString);

            var voices = new List<TtsVoice>();
            foreach (var voice in jsonDoc.RootElement.GetProperty("voices").EnumerateArray())
            {
                var voiceId = voice.GetProperty("name").GetString();
                var gender = voice.GetProperty("gender").GetString();
                var language = voice.GetProperty("language").GetString();
                var description = voice.GetProperty("description").GetString();

                voices.Add(new TtsVoice
                {
                    Id = voiceId ?? "",
                    Name = description ?? voiceId ?? "",
                    Gender = MapGender(gender),
                    Provider = "watson",
                    LanguageCodes = new List<LanguageInfo>
                    {
                        new LanguageInfo
                        {
                            Bcp47 = language ?? "",
                            Iso639_3 = language?.Split('-')[0] ?? "",
                            Display = language ?? ""
                        }
                    },
                    Description = description
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
                EngineName = "watson"
            };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                EngineName = "watson"
            };
        }
    }

    private List<TtsVoice> GetFallbackVoices()
    {
        return new List<TtsVoice>
        {
            new() { Id = "en-US_MichaelV3Voice", Name = "Michael", Gender = VoiceGender.Male, Provider = "watson", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "en-US_AllisonV3Voice", Name = "Allison", Gender = VoiceGender.Female, Provider = "watson", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        };
    }

    private static VoiceGender MapGender(string? gender)
    {
        return gender?.ToLowerInvariant() switch
        {
            "male" => VoiceGender.Male,
            "female" => VoiceGender.Female,
            _ => VoiceGender.Unknown
        };
    }
}