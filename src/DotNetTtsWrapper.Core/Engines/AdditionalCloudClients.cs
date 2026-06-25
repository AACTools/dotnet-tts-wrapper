using System.Net.Http.Json;
using System.Text.Json;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

public class WitAiTtsClient : HttpTtsClientBase
{
    private readonly WitAiCredentials _credentials;
    protected override string BaseEndpoint => "https://api.wit.ai/synthesize";
    protected override bool SupportsStreaming => false;

    public WitAiTtsClient(WitAiCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("Authorization", $"Bearer {_credentials.ApiKey}");

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = false,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = true,
            RequiresInternet = true
        };

        VoiceId = "Default";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            q = text,
            voice = options?.VoiceId ?? "Default",
            output = options?.Format switch
            {
                AudioFormat.Mp3 => "mp3",
                AudioFormat.Wav => "wav",
                _ => "mp3"
            }
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "";
    protected override string GetStreamingEndpoint(TtsOptions options) => "";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        // Wit.ai has limited voice options
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "Default", Name = "Default", Provider = "witai", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "witai" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "witai" };
        }
    }
}

public class GeminiTtsClient : HttpTtsClientBase
{
    private readonly GeminiCredentials _credentials;
    protected override string BaseEndpoint => "https://generativelanguage.googleapis.com/v1beta";
    protected override bool SupportsStreaming => false;

    public GeminiTtsClient(GeminiCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("x-goog-api-key", _credentials.ApiKey);

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = false,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = true,
            RequiresInternet = true
        };

        VoiceId = "Kore";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = text }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = options?.Format switch
                {
                    AudioFormat.Mp3 => "audio/mp3",
                    AudioFormat.Wav => "audio/wav",
                    _ => "audio/mp3"
                }
            }
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options)
    {
        return $"models/{_credentials.Model ?? "gemini-3.1-flash-tts-preview"}:generateAudio?key={_credentials.ApiKey}";
    }

    protected override string GetStreamingEndpoint(TtsOptions options) => GetSynthesisEndpoint(options);

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "Puck", Name = "Puck", Gender = VoiceGender.Unknown, Provider = "gemini", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "Kore", Name = "Kore", Gender = VoiceGender.Female, Provider = "gemini", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "gemini" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "gemini" };
        }
    }
}

public class CartesiaTtsClient : HttpTtsClientBase
{
    private readonly CartesiaCredentials _credentials;
    protected override string BaseEndpoint => "https://api.cartesia.ai";
    protected override bool SupportsStreaming => true;

    public CartesiaTtsClient(CartesiaCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("X-API-Key", _credentials.ApiKey);

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = true,
            RequiresInternet = true
        };

        VoiceId = "sonic-english";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            transcript = text,
            voice_id = options?.VoiceId ?? VoiceId ?? "sonic-english",
            output_format = new
            {
                container = "raw",
                encoding = "pcm_s16le",
                sample_rate = 24000
            }
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "tts/bytes";
    protected override string GetStreamingEndpoint(TtsOptions options) => "tts/bytes";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "sonic-english", Name = "Sonic English", Provider = "cartesia", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English" } } },
            new() { Id = "sonic-2", Name = "Sonic 2", Provider = "cartesia", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "cartesia" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "cartesia" };
        }
    }
}

public class DeepgramTtsClient : HttpTtsClientBase
{
    private readonly DeepgramCredentials _credentials;
    protected override string BaseEndpoint => "https://api.deepgram.com/v1";
    protected override bool SupportsStreaming => true;

    public DeepgramTtsClient(DeepgramCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("Authorization", $"Token {_credentials.ApiKey}");

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = true,
            RequiresInternet = true
        };

        VoiceId = "aura-asteria-en";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            text = text,
            model = options?.VoiceId ?? VoiceId ?? "aura-asteria-en"
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "speak";
    protected override string GetStreamingEndpoint(TtsOptions options) => "speak";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "aura-asteria-en", Name = "Asteria", Gender = VoiceGender.Female, Provider = "deepgram", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "aura-luna-en", Name = "Luna", Gender = VoiceGender.Female, Provider = "deepgram", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "deepgram" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "deepgram" };
        }
    }
}

public class HumeTtsClient : HttpTtsClientBase
{
    private readonly HumeCredentials _credentials;
    protected override string BaseEndpoint => "https://api.hume.ai/v0/tts";
    protected override bool SupportsStreaming => true;

    public HumeTtsClient(HumeCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("X-API-Key", _credentials.ApiKey);

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = true,
            RequiresInternet = true
        };

        VoiceId = "ito";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            utterance = text,
            voice = new { name = options?.VoiceId ?? VoiceId ?? "ito" }
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "";
    protected override string GetStreamingEndpoint(TtsOptions options) => "";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "ito", Name = "Ito", Gender = VoiceGender.Unknown, Provider = "hume", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "hume" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "hume" };
        }
    }
}