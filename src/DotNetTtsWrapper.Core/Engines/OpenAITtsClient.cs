using System.Text.Json;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// OpenAI TTS Client
/// Uses OpenAI's Text-to-Speech API
/// </summary>
public class OpenAITtsClient : HttpTtsClientBase
{
    private readonly OpenAICredentials _credentials;
    private const string OpenAiApiUrl = "https://api.openai.com/v1";

    protected override string BaseEndpoint => OpenAiApiUrl;
    protected override bool SupportsStreaming => true;

    public OpenAITtsClient(OpenAICredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetAuthentication("Bearer", _credentials.ApiKey);

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

        VoiceId = "alloy"; // Default voice
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            model = "tts-1", // or tts-1-hd for higher quality
            input = text,
            voice = options.VoiceId ?? VoiceId ?? "alloy",
            response_format = options.Format switch
            {
                AudioFormat.Mp3 => "mp3",
                AudioFormat.Opus => "opus",
                AudioFormat.Aac => "aac",
                AudioFormat.Flac => "flac",
                _ => "mp3" // default
            },
            speed = GetSpeedValue(options)
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options)
    {
        return "audio/speech";
    }

    protected override string GetStreamingEndpoint(TtsOptions options)
    {
        return "audio/speech"; // OpenAI supports streaming via the same endpoint
    }

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        // OpenAI has a fixed set of voices, so we return them directly
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "alloy", Name = "Alloy", Gender = VoiceGender.Unknown, Provider = "openai", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "echo", Name = "Echo", Gender = VoiceGender.Male, Provider = "openai", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "fable", Name = "Fable", Gender = VoiceGender.Male, Provider = "openai", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "onyx", Name = "Onyx", Gender = VoiceGender.Male, Provider = "openai", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "nova", Name = "Nova", Gender = VoiceGender.Female, Provider = "openai", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "shimmer", Name = "Shimmer", Gender = VoiceGender.Female, Provider = "openai", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            // Try to get voices as a simple validation check
            var voices = await GetVoicesAsync();
            return new CredentialsValidationResult
            {
                IsValid = voices.Count > 0,
                AvailableVoiceCount = voices.Count,
                EngineName = "openai"
            };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                EngineName = "openai"
            };
        }
    }

    private double GetSpeedValue(TtsOptions options)
    {
        // Map rate enum to speed value (0.25 to 4.0)
        var rate = options.Rate ?? Properties.Rate ?? SpeechRate.Medium;
        return rate switch
        {
            SpeechRate.XSlow => 0.5,
            SpeechRate.Slow => 0.75,
            SpeechRate.Medium => 1.0,
            SpeechRate.Fast => 1.25,
            SpeechRate.XFast => 1.5,
            _ => 1.0
        };
    }
}