using System.Net.Http.Json;
using System.Text.Json;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// PlayHT TTS Client
/// Uses PlayHT API for high-quality text-to-speech
/// </summary>
public class PlayHtTtsClient : HttpTtsClientBase
{
    private readonly PlayHtCredentials _credentials;

    protected override string BaseEndpoint => "https://api.play.ht/api/v2";
    protected override bool SupportsStreaming => true;

    public PlayHtTtsClient(PlayHtCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("AUTHORIZATION", _credentials.ApiKey);
        SetApiKeyHeader("X-USER-ID", _credentials.UserId);

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = true,
            RequiresInternet = true,
            IsWindowsSupported = true,
            IsLinuxSupported = true,
            IsMacOsSupported = true
        };

        VoiceId = "s3://voice-cloning/zero_shot_thoughtful/67687c1f-614c-4a17-b943-fbe015e3e5e4/manifest.json";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            text = text,
            voice_id = options?.VoiceId ?? VoiceId ?? "s3://voice-cloning/zero_shot_thoughtful/67687c1f-614c-4a17-b943-fbe015e3e5e4/manifest.json",
            output_format = options?.Format switch
            {
                AudioFormat.Mp3 => "mp3",
                AudioFormat.Wav => "wav",
                _ => "mp3"
            }
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options)
    {
        return "tts";
    }

    protected override string GetStreamingEndpoint(TtsOptions options)
    {
        return "tts/stream"; // PlayHT supports streaming
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
                var voiceId = voice.GetProperty("voice_id").GetString();
                var voiceName = voice.GetProperty("name").GetString();
                var gender = voice.GetProperty("gender").GetString();

                voices.Add(new TtsVoice
                {
                    Id = voiceId ?? "",
                    Name = voiceName ?? "",
                    Gender = MapGender(gender),
                    Provider = "playht",
                    LanguageCodes = new List<LanguageInfo>
                    {
                        new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" }
                    }
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
                EngineName = "playht"
            };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                EngineName = "playht"
            };
        }
    }

    private List<TtsVoice> GetFallbackVoices()
    {
        return new List<TtsVoice>
        {
            new() { Id = "s3://voice-cloning/zero_shot_thoughtful/67687c1f-614c-4a17-b943-fbe015e3e5e4/manifest.json", Name = "Thoughtful", Gender = VoiceGender.Male, Provider = "playht" },
            new() { Id = "s3://mary-zone/voices/8ed7d877-4036-4388-bb27-b8d1fb78d6a6/primary/manifest.json", Name = "Scarlett", Gender = VoiceGender.Female, Provider = "playht" }
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