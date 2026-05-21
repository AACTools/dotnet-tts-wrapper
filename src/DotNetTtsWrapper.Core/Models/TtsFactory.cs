using DotNetTtsWrapper.Engines;

namespace DotNetTtsWrapper.Models;

/// <summary>
/// Factory for creating TTS clients
/// </summary>
public static class TtsFactory
{
    /// <summary>
    /// Create a TTS client for the specified engine
    /// </summary>
    public static AbstractTtsClient CreateClient(string engine, ITtsCredentials? credentials = null)
    {
        if (string.IsNullOrWhiteSpace(engine))
            throw new ArgumentException("Engine name cannot be empty", nameof(engine));

        var normalizedEngine = engine.ToLowerInvariant().Trim().Replace(" ", "").Replace("-", "");

        AbstractTtsClient client = normalizedEngine switch
        {
            // Windows engines
            "sapi" => new SapiTtsClient(),
            "azure" => new AzureSdkTtsClient(credentials as AzureCredentials),
            "sherpaonnx" => new SherpaOnnxTtsClient(credentials as SherpaOnnxCredentials),

            // Cloud engines
            "google" => new GoogleTtsClient(credentials as GoogleCredentials),
            "polly" => new PollyTtsClient(credentials as PollyCredentials),
            "openai" => new OpenAITtsClient(credentials as OpenAICredentials),
            "elevenlabs" => new ElevenLabsTtsClient(credentials as ElevenLabsCredentials),
            "watson" => new WatsonTtsClient(credentials as WatsonCredentials),
            "playht" => new PlayHtTtsClient(credentials as PlayHtCredentials),
            "witai" => new WitAiTtsClient(credentials as WitAiCredentials),
            "gemini" => new GeminiTtsClient(credentials as GeminiCredentials),
            "cartesia" => new CartesiaTtsClient(credentials as CartesiaCredentials),
            "deepgram" => new DeepgramTtsClient(credentials as DeepgramCredentials),
            "hume" => new HumeTtsClient(credentials as HumeCredentials),
            "xai" => new XaiTtsClient(credentials as XaiCredentials),
            "fishaudio" => new FishAudioTtsClient(credentials as FishAudioCredentials),
            "mistral" => new MistralTtsClient(credentials as MistralCredentials),
            "murf" => new MurfTtsClient(credentials as MurfCredentials),
            "unrealspeech" => new UnrealSpeechTtsClient(credentials as UnrealSpeechCredentials),
            "resemble" => new ResembleTtsClient(credentials as ResembleCredentials),
            "upliftai" => new UpliftAiTtsClient(credentials as UpliftAiCredentials),
            "modelslab" => new ModelsLabTtsClient(credentials as ModelsLabCredentials),

            // Local engines (will be implemented later)
            "espeak" => throw new NotImplementedException("eSpeak support coming soon"),
            "cerevoice" => throw new NotImplementedException("CereVoice support coming soon"),

            _ => throw new NotSupportedException($"Engine '{engine}' is not supported")
        };

        // Apply properties from credentials if available
        if (credentials != null && client != null)
        {
            ApplyPropertiesFromCredentials(client, credentials);
        }

        return client;
    }

    /// <summary>
    /// Get all supported engine names
    /// </summary>
    public static IEnumerable<string> GetSupportedEngines()
    {
        return new[]
        {
            // Windows engines
            "sapi",
            "azure",

            // Local engines
            "sherpaonnx",

            // Cloud engines
            "google", "polly", "openai", "elevenlabs", "watson", "playht", "witai",
            "gemini", "cartesia", "deepgram", "hume", "xai", "fishaudio", "mistral",
            "murf", "unrealspeech", "resemble", "upliftai", "modelslab"
        };
    }

    /// <summary>
    /// Get engines that support specific capabilities
    /// </summary>
    public static IEnumerable<string> GetEnginesWithCapability(Func<EngineCapabilities, bool> capabilityFilter)
    {
        var engines = new List<string>();
        var supportedEngines = GetSupportedEngines();

        foreach (var engine in supportedEngines)
        {
            try
            {
                var client = CreateClient(engine);
                if (capabilityFilter(client.Capabilities))
                {
                    engines.Add(engine);
                }

                if (client is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch
            {
                // Skip engines that can't be instantiated
                continue;
            }
        }

        return engines;
    }

    private static void ApplyPropertiesFromCredentials(AbstractTtsClient client, ITtsCredentials credentials)
    {
        // This is a placeholder for future property application logic
        // Different credential types may have different property structures
        // For now, we'll keep it simple
    }
}