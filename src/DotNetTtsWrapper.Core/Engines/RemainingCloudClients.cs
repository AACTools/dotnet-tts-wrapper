using System.Net.Http.Json;
using System.Text.Json;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

public class XaiTtsClient : HttpTtsClientBase
{
    private readonly XaiCredentials _credentials;
    protected override string BaseEndpoint => "https://api.x.ai/v1/audio";
    protected override bool SupportsStreaming => true;

    public XaiTtsClient(XaiCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("Authorization", $"Bearer {_credentials.ApiKey}");

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = false,
            RequiresInternet = true
        };

        VoiceId = "grok-tts-1";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            text = text,
            voice = options?.VoiceId ?? VoiceId ?? "grok-tts-1"
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "speech";
    protected override string GetStreamingEndpoint(TtsOptions options) => "speech";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "grok-tts-1", Name = "Grok TTS 1", Provider = "xai", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "xai" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "xai" };
        }
    }
}

public class FishAudioTtsClient : HttpTtsClientBase
{
    private readonly FishAudioCredentials _credentials;
    protected override string BaseEndpoint => "https://api.fish.audio";
    protected override bool SupportsStreaming => true;

    public FishAudioTtsClient(FishAudioCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("Authorization", $"Bearer {_credentials.ApiKey}");

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = false,
            RequiresInternet = true
        };

        VoiceId = "9f45e5ee00a6404a947ee61e6ee749b3";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            text = text,
            voice_id = options?.VoiceId ?? VoiceId ?? "9f45e5ee00a6404a947ee61e6ee749b3",
            format = options?.Format switch
            {
                AudioFormat.Mp3 => "mp3",
                AudioFormat.Wav => "wav",
                _ => "mp3"
            }
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "v1/tts";
    protected override string GetStreamingEndpoint(TtsOptions options) => "v1/tts";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "9f45e5ee00a6404a947ee61e6ee749b3", Name = "Default Voice", Provider = "fishaudio", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "fishaudio" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "fishaudio" };
        }
    }
}

public class MistralTtsClient : HttpTtsClientBase
{
    private readonly MistralCredentials _credentials;
    protected override string BaseEndpoint => "https://api.mistral.ai/v1/tts";
    protected override bool SupportsStreaming => true;

    public MistralTtsClient(MistralCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("Authorization", $"Bearer {_credentials.ApiKey}");

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = false,
            RequiresInternet = true
        };

        VoiceId = "en-US-emma";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            model = "tts-1",
            text = text,
            voice = options?.VoiceId ?? VoiceId ?? "en-US-emma"
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "";
    protected override string GetStreamingEndpoint(TtsOptions options) => "";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "en-US-emma", Name = "Emma", Gender = VoiceGender.Female, Provider = "mistral", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "mistral" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "mistral" };
        }
    }
}

public class MurfTtsClient : HttpTtsClientBase
{
    private readonly MurfCredentials _credentials;
    protected override string BaseEndpoint => "https://api.murf.ai/v1/tts";
    protected override bool SupportsStreaming => true;

    public MurfTtsClient(MurfCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("Authorization", $"Bearer {_credentials.ApiKey}");

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = false,
            RequiresInternet = true
        };

        VoiceId = "en-US-natalie";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            text = text,
            voice = options?.VoiceId ?? VoiceId ?? "en-US-natalie",
            format = options?.Format switch
            {
                AudioFormat.Mp3 => "MP3",
                AudioFormat.Wav => "WAV",
                _ => "MP3"
            }
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "generate";
    protected override string GetStreamingEndpoint(TtsOptions options) => "generate";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "en-US-natalie", Name = "Natalie", Gender = VoiceGender.Female, Provider = "murf", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "murf" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "murf" };
        }
    }
}

public class UnrealSpeechTtsClient : HttpTtsClientBase
{
    private readonly UnrealSpeechCredentials _credentials;
    protected override string BaseEndpoint => "https://api.v2.unrealspeech.com/stream";
    protected override bool SupportsStreaming => true;

    public UnrealSpeechTtsClient(UnrealSpeechCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("Authorization", $"Bearer {_credentials.ApiKey}");

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = false,
            RequiresInternet = true
        };

        VoiceId = "Scarlett";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            Text = text,
            VoiceId = options?.VoiceId ?? VoiceId ?? "Scarlett",
            Bitrate = "192000",
            Speed = "0",
            Pitch = "1.0",
            Codec = "libmp3lame"
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "";
    protected override string GetStreamingEndpoint(TtsOptions options) => "";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "Scarlett", Name = "Scarlett", Gender = VoiceGender.Female, Provider = "unrealspeech", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } },
            new() { Id = "Dan", Name = "Dan", Gender = VoiceGender.Male, Provider = "unrealspeech", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "unrealspeech" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "unrealspeech" };
        }
    }
}

public class ResembleTtsClient : HttpTtsClientBase
{
    private readonly ResembleCredentials _credentials;
    protected override string BaseEndpoint => "https://app.resemble.ai/api/v2";
    protected override bool SupportsStreaming => true;

    public ResembleTtsClient(ResembleCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("Authorization", $"Token {_credentials.ApiKey}");

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = false,
            RequiresInternet = true
        };

        VoiceId = "default";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            text = text,
            voice_id = options?.VoiceId ?? VoiceId ?? "default",
            output_format = options?.Format switch
            {
                AudioFormat.Mp3 => "mp3",
                AudioFormat.Wav => "wav",
                _ => "mp3"
            }
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "synthesize";
    protected override string GetStreamingEndpoint(TtsOptions options) => "synthesize";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "default", Name = "Default", Provider = "resemble", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "resemble" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "resemble" };
        }
    }
}

public class UpliftAiTtsClient : HttpTtsClientBase
{
    private readonly UpliftAiCredentials _credentials;
    protected override string BaseEndpoint => "https://api.uplift.ai/v1/tts";
    protected override bool SupportsStreaming => true;

    public UpliftAiTtsClient(UpliftAiCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("Authorization", $"Bearer {_credentials.ApiKey}");

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = false,
            RequiresInternet = true
        };

        VoiceId = "default";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            text = text,
            voice_id = options?.VoiceId ?? VoiceId ?? "default"
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "synthesize";
    protected override string GetStreamingEndpoint(TtsOptions options) => "synthesize";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "default", Name = "Default", Provider = "upliftai", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "upliftai" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "upliftai" };
        }
    }
}

public class ModelsLabTtsClient : HttpTtsClientBase
{
    private readonly ModelsLabCredentials _credentials;
    protected override string BaseEndpoint => "https://modelslab.com/api/v1/tts";
    protected override bool SupportsStreaming => true;

    public ModelsLabTtsClient(ModelsLabCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        SetApiKeyHeader("Authorization", $"Bearer {_credentials.ApiKey}");

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = true,
            SupportsWordTimings = false,
            SupportsSsml = false,
            SupportsSpeechMarkdown = false,
            RequiresInternet = true
        };

        VoiceId = "default";
    }

    protected override async Task<object> BuildSynthesisPayload(string text, TtsOptions options)
    {
        return new
        {
            text = text,
            voice_id = options?.VoiceId ?? VoiceId ?? "default"
        };
    }

    protected override string GetSynthesisEndpoint(TtsOptions options) => "synthesize";
    protected override string GetStreamingEndpoint(TtsOptions options) => "synthesize";

    protected override async Task<List<TtsVoice>> GetVoicesFromApiAsync()
    {
        return await Task.FromResult(new List<TtsVoice>
        {
            new() { Id = "default", Name = "Default", Provider = "modelslab", LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } } }
        });
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await SynthToBytesAsync("test", new TtsOptions());
            return new CredentialsValidationResult { IsValid = true, EngineName = "modelslab" };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult { IsValid = false, ErrorMessage = ex.Message, EngineName = "modelslab" };
        }
    }
}