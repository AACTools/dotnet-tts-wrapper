using System.Net.Http.Json;
using System.Text.Json;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// Azure Cognitive Services TTS Client
/// Uses Azure Speech Services REST API
/// </summary>
public class AzureTtsClient : HttpTtsClientBase
{
    private readonly AzureCredentials _credentials;
    private const string AzureTtsUrl = "https://REGION.tts.speech.microsoft.com/cognitiveservices/v1";

    protected override string BaseEndpoint => $"https://{_credentials.Region}.tts.speech.microsoft.com/cognitiveservices";
    protected override bool SupportsStreaming => true;

    public AzureTtsClient(AzureCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("Ocp-Apim-Subscription-Key", _credentials.SubscriptionKey);
        _httpClient.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", "audio-24khz-160kbitrate-mono-mp3");

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

        VoiceId = "en-US-AriaNeural";
    }

    public override string GetSpeechMarkdownPlatform()
    {
        return global::SpeechMarkdown.Platform.MicrosoftAzure;
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        // For Azure, we need to create SSML
        if (options?.RawSsml == true || text.TrimStart().StartsWith("<speak"))
        {
            return text; // Return SSML as-is
        }

        // Build SSML for Azure
        var ssml = CreateSsml(text, options);
        return ssml;
    }

    protected override string GetSynthesisEndpoint(TtsOptions options)
    {
        return "v1";
    }

    protected override string GetStreamingEndpoint(TtsOptions options)
    {
        return "v1";
    }

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseEndpoint}/voices/list");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonString);

            var voices = new List<TtsVoice>();
            foreach (var voice in jsonDoc.RootElement.EnumerateArray())
            {
                var voiceId = voice.GetProperty("ShortName").GetString();
                var voiceName = voice.GetProperty("DisplayName").GetString();
                var gender = voice.GetProperty("Gender").GetString();
                var locale = voice.GetProperty("Locale").GetString();

                voices.Add(new TtsVoice
                {
                    Id = voiceId ?? "",
                    Name = voiceName ?? "",
                    Gender = MapGender(gender),
                    Provider = "azure",
                    LanguageCodes = new List<LanguageInfo>
                    {
                        new LanguageInfo
                        {
                            Bcp47 = locale ?? "",
                            Iso639_3 = locale?.Split('-')[0] ?? "",
                            Display = locale ?? ""
                        }
                    }
                });
            }

            return voices;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve Azure voices", ex);
        }
    }

    public override async Task<TtsSynthesisResult> SynthToBytesAsync(string text, TtsOptions? options = null)
    {
        var preparedText = await PrepareTextAsync(text, options);
        var ssml = BuildSsmlForAzure(preparedText, options);

        var content = new StringContent(ssml, System.Text.Encoding.UTF8, "application/ssml+xml");
        var url = $"{BaseEndpoint}/{GetSynthesisEndpoint(options!)}";

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var audioData = await response.Content.ReadAsByteArrayAsync();
        return new TtsSynthesisResult
        {
            AudioData = audioData,
            Format = AudioFormat.Mp3,
            SampleRate = 24000,
            Channels = 1
        };
    }

    private string BuildSsmlForAzure(string text, TtsOptions? options)
    {
        if (options?.RawSsml == true || text.TrimStart().StartsWith("<speak"))
            return text;

        var voice = options?.VoiceId ?? VoiceId ?? "en-US-AriaNeural";
        var lang = voice.Length >= 5 ? voice.Substring(0, 5) : "en-US";
        return $"<speak version='1.0' xml:lang='{lang}'><voice name='{voice}'>{System.Security.SecurityElement.Escape(text)}</voice></speak>";
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
                EngineName = "azure"
            };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                EngineName = "azure"
            };
        }
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