using System.Net.Http.Json;
using System.Text.Json;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// AWS Polly TTS Client
/// Uses AWS Polly REST API
/// </summary>
public class PollyTtsClient : HttpTtsClientBase
{
    private readonly PollyCredentials _credentials;

    protected override string BaseEndpoint => $"https://polly.{_credentials.Region}.amazonaws.com";
    protected override bool SupportsStreaming => true;

    public PollyTtsClient(PollyCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));

        // Set AWS signature headers
        SetApiKeyHeader("X-Amz-Target", "Amazon.Polly_20160620");
        SetAuthentication("AWS4-HMAC-SHA256", $"{_credentials.AccessKeyId}/{_credentials.Region}/polly/aws4_request");

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

        VoiceId = "Joanna";
    }

    public override string GetSpeechMarkdownPlatform()
    {
        return global::SpeechMarkdown.Platform.AmazonAlexa;
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            Text = text,
            OutputFormat = options?.Format switch
            {
                AudioFormat.Mp3 => "mp3",
                AudioFormat.Wav => "pcm",
                AudioFormat.Ogg => "ogg_vorbis",
                _ => "mp3"
            },
            VoiceId = options?.VoiceId ?? VoiceId ?? "Joanna",
            SampleRate = "16000",
            TextType = options?.RawSsml == true || text.TrimStart().StartsWith("<speak") ? "ssml" : "text"
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options)
    {
        return "v1/speech";
    }

    protected override string GetStreamingEndpoint(TtsOptions options)
    {
        return "v1/speech";
    }

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{BaseEndpoint}/v1/voices",
                JsonContent.Create(new { })
            );
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonString);

            var voices = new List<TtsVoice>();
            if (jsonDoc.RootElement.TryGetProperty("Voices", out var voicesArray))
            {
                foreach (var voice in voicesArray.EnumerateArray())
                {
                    var voiceId = voice.GetProperty("Id").GetString();
                    var gender = voice.GetProperty("Gender").GetString();

                    voices.Add(new TtsVoice
                    {
                        Id = voiceId ?? "",
                        Name = voiceId ?? "",
                        Gender = MapGender(gender),
                        Provider = "polly",
                        LanguageCodes = new List<LanguageInfo>
                        {
                            new LanguageInfo
                            {
                                Bcp47 = voice.GetProperty("LanguageCode").GetString() ?? "",
                                Iso639_3 = voice.GetProperty("LanguageCode").GetString()?.Split('-')[0] ?? "",
                                Display = voice.GetProperty("LanguageName").GetString() ?? ""
                            }
                        }
                    });
                }
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
                EngineName = "polly"
            };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                EngineName = "polly"
            };
        }
    }

    private List<TtsVoice> GetFallbackVoices()
    {
        return new List<TtsVoice>
        {
            new() { Id = "Joanna", Name = "Joanna", Gender = VoiceGender.Female, Provider = "polly", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "Matthew", Name = "Matthew", Gender = VoiceGender.Male, Provider = "polly", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "Kimberly", Name = "Kimberly", Gender = VoiceGender.Female, Provider = "polly", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        };
    }

    private static VoiceGender MapGender(string? gender)
    {
        return gender?.ToLowerInvariant() switch
        {
            "female" => VoiceGender.Female,
            "male" => VoiceGender.Male,
            _ => VoiceGender.Unknown
        };
    }
}