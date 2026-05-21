using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using DotNetTtsWrapper.Events;
using DotNetTtsWrapper.Models;
using SherpaOnnx;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// SherpaOnnx TTS Client - Local offline TTS supporting Kokoro, Matcha, and VITS models
/// </summary>
public class SherpaOnnxTtsClient : AbstractTtsClient
{
    private readonly SherpaOnnxCredentials _credentials;
    private OfflineTts? _tts;
    private string? _currentModelId;
    private bool _isInitialized = false;
    private Dictionary<string, SherpaOnnxModelConfig>? _modelsConfig;

    public SherpaOnnxTtsClient(SherpaOnnxCredentials credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = false, // SherpaOnnx is batch-oriented
            SupportsWordTimings = false, // No word boundary events supported
            SupportsSsml = false, // Plain text only
            SupportsSpeechMarkdown = false,
            RequiresInternet = false, // Fully offline
            IsWindowsSupported = true,
            IsLinuxSupported = true,
            IsMacOsSupported = true
        };

        VoiceId = "vits-piper-en_US-amy-low"; // Default voice

        // Load models configuration
        LoadModelsConfiguration();
    }

    private void LoadModelsConfiguration()
    {
        try
        {
            var assembly = typeof(SherpaOnnxTtsClient).Assembly;
            var resourceStream = assembly.GetManifestResourceStream("DotNetTtsWrapper.Models.merged_models.json");

            if (resourceStream != null)
            {
                using var reader = new StreamReader(resourceStream);
                var json = reader.ReadToEnd();
                _modelsConfig = JsonSerializer.Deserialize<Dictionary<string, SherpaOnnxModelConfig>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load SherpaOnnx models configuration: {ex.Message}");
            _modelsConfig = new Dictionary<string, SherpaOnnxModelConfig>();
        }
    }

    private async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        var modelId = _credentials.ModelId ?? VoiceId ?? "vits-piper-en_US-amy-low";
        _currentModelId = modelId;

        // Get model configuration
        var modelConfig = await GetModelConfigurationAsync(modelId);

        // Create SherpaOnnx configuration
        var config = new OfflineTtsConfig();

        if (modelConfig.ModelType == "kokoro")
        {
            // Kokoro model configuration
            config.Model.Kokoro.Model = modelConfig.ModelPath;
            config.Model.Kokoro.Voices = modelConfig.VoicesPath;
            config.Model.Kokoro.Tokens = modelConfig.TokensPath;
            config.Model.Kokoro.DataDir = modelConfig.DataDir;
            config.Model.Kokoro.Lexicon = modelConfig.LexiconPath;
        }
        else if (modelConfig.ModelType == "matcha")
        {
            // Matcha model configuration
            config.Model.Matcha.AcousticModel = modelConfig.ModelPath;
            config.Model.Matcha.Vocoder = modelConfig.VocoderPath;
            config.Model.Matcha.Tokens = modelConfig.TokensPath;
            config.Model.Matcha.Lexicon = modelConfig.LexiconPath;
            config.Model.Matcha.DataDir = modelConfig.DataDir;
        }
        else // Default to VITS
        {
            // VITS model configuration
            config.Model.Vits.Model = modelConfig.ModelPath;
            config.Model.Vits.Tokens = modelConfig.TokensPath;
            config.Model.Vits.Lexicon = modelConfig.LexiconPath;
            config.Model.Vits.DataDir = modelConfig.DataDir;
        }

        config.Model.NumThreads = 1;
        config.Model.Debug = 0;
        config.Model.Provider = "cpu";
        config.RuleFsts = modelConfig.RuleFsts;
        config.RuleFars = modelConfig.RuleFars;
        config.MaxNumSentences = 1;

        // Create TTS instance
        _tts = new OfflineTts(config);
        _isInitialized = true;
    }

    private async Task<ModelConfiguration> GetModelConfigurationAsync(string modelId)
    {
        // Check if we have a custom model path
        if (!string.IsNullOrEmpty(_credentials.ModelPath))
        {
            return new ModelConfiguration
            {
                ModelType = DetermineModelTypeFromPath(_credentials.ModelPath),
                ModelPath = _credentials.ModelPath,
                TokensPath = Path.Combine(_credentials.ModelPath, "tokens.txt"),
                DataDir = Path.Combine(_credentials.ModelPath, "espeak-ng-data"),
                LexiconPath = Path.Combine(_credentials.ModelPath, "lexicon.txt")
            };
        }

        // Check if model exists in our configuration
        if (_modelsConfig != null && _modelsConfig.TryGetValue(modelId, out var modelConfig))
        {
            // Use default model directory
            var baseDir = _credentials.BaseModelsDir ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".dotnet-tts-wrapper", "models");

            var modelDir = Path.Combine(baseDir, modelId);

            return new ModelConfiguration
            {
                ModelType = modelConfig.ModelType ?? DetermineModelType(modelId),
                ModelPath = Path.Combine(modelDir, "model.onnx"),
                TokensPath = Path.Combine(modelDir, "tokens.txt"),
                DataDir = Path.Combine(modelDir, "espeak-ng-data"),
                LexiconPath = Path.Combine(modelDir, "lexicon.txt"),
                VoicesPath = Path.Combine(modelDir, "voices.bin"),
                VocoderPath = Path.Combine(modelDir, "vocoder.onnx"),
                Url = modelConfig.Url
            };
        }

        // Fallback to basic configuration
        var defaultBaseDir = _credentials.BaseModelsDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dotnet-tts-wrapper", "models");

        var defaultModelDir = Path.Combine(defaultBaseDir, modelId);

        return new ModelConfiguration
        {
            ModelType = DetermineModelType(modelId),
            ModelPath = Path.Combine(defaultModelDir, "model.onnx"),
            TokensPath = Path.Combine(defaultModelDir, "tokens.txt"),
            DataDir = Path.Combine(defaultModelDir, "espeak-ng-data"),
            LexiconPath = Path.Combine(defaultModelDir, "lexicon.txt"),
            VoicesPath = Path.Combine(defaultModelDir, "voices.bin"),
            VocoderPath = Path.Combine(defaultModelDir, "vocoder.onnx")
        };
    }

    private string DetermineModelType(string modelId)
    {
        if (modelId.StartsWith("kokoro-"))
            return "kokoro";
        if (modelId.StartsWith("matcha-"))
            return "matcha";
        return "vits"; // Default
    }

    private string DetermineModelTypeFromPath(string path)
    {
        var dirName = Path.GetDirectoryName(path) ?? "";
        if (dirName.Contains("kokoro"))
            return "kokoro";
        if (dirName.Contains("matcha"))
            return "matcha";
        return "vits";
    }

    public override async Task<List<TtsVoice>> GetVoicesAsync()
    {
        return await Task.FromResult(GetVoicesFromModelsConfig());
    }

    private List<TtsVoice> GetVoicesFromModelsConfig()
    {
        var voices = new List<TtsVoice>();

        if (_modelsConfig == null || _modelsConfig.Count == 0)
        {
            // Fallback to default voices if config loading failed
            return GetDefaultVoices();
        }

        foreach (var kvp in _modelsConfig)
        {
            var modelId = kvp.Key;
            var config = kvp.Value;

            voices.Add(new TtsVoice
            {
                Id = modelId,
                Name = config.Name ?? modelId,
                Gender = MapGender(config.Gender),
                Provider = "sherpa-onnx",
                LanguageCodes = GetLanguageCodes(config.Language),
                Description = $"{config.ModelType} model - {config.Developer}"
            });
        }

        return voices;
    }

    private List<TtsVoice> GetDefaultVoices()
    {
        return new List<TtsVoice>
        {
            new() {
                Id = "kokoro-en-en-19",
                Name = "Kokoro English (19 voices)",
                Gender = VoiceGender.Female,
                Provider = "sherpa-onnx",
                LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } }
            },
            new() {
                Id = "vits-piper-en_US-amy-low",
                Name = "Piper Amy (Low Quality)",
                Gender = VoiceGender.Female,
                Provider = "sherpa-onnx",
                LanguageCodes = { new LanguageInfo { Bcp47 = "en-US", Iso639_3 = "en", Display = "English (US)" } }
            }
        };
    }

    private VoiceGender MapGender(string? gender)
    {
        return gender?.ToLowerInvariant() switch
        {
            "male" => VoiceGender.Male,
            "female" => VoiceGender.Female,
            _ => VoiceGender.Unknown
        };
    }

    private List<LanguageInfo> GetLanguageCodes(List<SherpaOnnxLanguage>? languages)
    {
        var languageCodes = new List<LanguageInfo>();

        if (languages == null)
            return languageCodes;

        foreach (var lang in languages)
        {
            languageCodes.Add(new LanguageInfo
            {
                Bcp47 = lang.LangCode ?? "en-US",
                Iso639_3 = lang.LanguageName?.Substring(0, 2) ?? "en",
                Display = lang.LanguageName ?? "English"
            });
        }

        return languageCodes;
    }

    public override async Task<List<TtsVoice>> GetVoicesByLanguageAsync(string languageCode)
    {
        var allVoices = await GetVoicesAsync();
        return allVoices
            .Where(v => v.LanguageCodes.Any(l =>
                l.Bcp47.Equals(languageCode, StringComparison.OrdinalIgnoreCase) ||
                l.Iso639_3.Equals(languageCode, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public override async Task<TtsSynthesisResult> SynthToBytesAsync(string text, TtsOptions? options = null)
    {
        await InitializeAsync();

        if (_tts == null)
            throw new InvalidOperationException("SherpaOnnx TTS not initialized");

        options ??= new TtsOptions();
        var preparedText = await PrepareTextAsync(text, options);

        // Configure generation
        var genConfig = new OfflineTtsGenerationConfig
        {
            Sid = 0, // Default speaker ID
            Speed = 1.0f, // Normal speed
            SilenceScale = 0.2f
        };

        // Generate audio
        var audio = _tts.GenerateWithConfig(preparedText, genConfig, null);

        // Save to temp file and read back
        var tempFile = Path.GetTempFileName();
        audio.SaveToWaveFile(tempFile);
        var audioData = await File.ReadAllBytesAsync(tempFile);

        // Clean up temp file
        if (File.Exists(tempFile))
            File.Delete(tempFile);

        return new TtsSynthesisResult
        {
            AudioData = audioData,
            WordTimings = new List<WordTimingEventArgs>(), // No word boundaries
            Format = AudioFormat.Wav,
            SampleRate = 24000,
            Channels = 1
        };
    }

    public override async Task<StreamingTtsResult> SynthToStreamAsync(string text, TtsOptions? options = null)
    {
        // SherpaOnnx doesn't support true streaming, so we'll batch the entire synthesis
        var synthesisResult = await SynthToBytesAsync(text, options);

        var streamingResult = new StreamingTtsResult
        {
            Format = synthesisResult.Format,
            SampleRate = synthesisResult.SampleRate,
            Channels = synthesisResult.Channels,
            WordTimings = synthesisResult.WordTimings,
            FinalAudioData = synthesisResult.AudioData
        };

        // Create pseudo-stream from complete audio
        streamingResult.AudioStream = CreateAudioChunkStream(synthesisResult.AudioData, synthesisResult.Format);

        return streamingResult;
    }

    public override async Task SynthToFileAsync(string text, string outputPath, AudioFormat format = AudioFormat.Wav, TtsOptions? options = null)
    {
        await InitializeAsync();

        if (_tts == null)
            throw new InvalidOperationException("SherpaOnnx TTS not initialized");

        options ??= new TtsOptions();
        var preparedText = await PrepareTextAsync(text, options);

        var genConfig = new OfflineTtsGenerationConfig
        {
            Sid = 0, // Default speaker ID
            Speed = 1.0f,
            SilenceScale = 0.2f
        };

        var audio = _tts.GenerateWithConfig(preparedText, genConfig, null);
        audio.SaveToWaveFile(outputPath);
    }

    public override async Task SpeakAsync(string text, TtsOptions? options = null)
    {
        // Synthesize to temp file and play using system audio
        var tempFile = Path.Combine(Path.GetTempPath(), $"tts_{Guid.NewGuid()}.wav");

        try
        {
            await SynthToFileAsync(text, tempFile, AudioFormat.Wav, options);

            // Use platform-specific audio playback
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Windows: Use System.Media.SoundPlayer or Windows Media Player
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c start \"\" \"{tempFile}\"",
                    UseShellExecute = true
                })?.WaitForExit();
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // Linux/macOS: Use afplay or paplay
                var player = Environment.OSVersion.VersionString.Contains("Darwin") ? "afplay" : "paplay";
                Process.Start(new ProcessStartInfo
                {
                    FileName = player,
                    Arguments = $"\"{tempFile}\"",
                    UseShellExecute = true
                })?.WaitForExit();
            }
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public override Task SpeakStreamedAsync(string text, Action<WordTimingEventArgs>? wordCallback = null, TtsOptions? options = null)
    {
        // SherpaOnnx doesn't support streaming or word boundaries
        return SpeakAsync(text, options);
    }

    public override void Pause()
    {
        // SherpaOnnx doesn't support pause/resume for playback
        throw new NotSupportedException("Pause/resume is not supported by SherpaOnnx TTS");
    }

    public override void Resume()
    {
        throw new NotSupportedException("Pause/resume is not supported by SherpaOnnx TTS");
    }

    public override void Stop()
    {
        // SherpaOnnx synthesis is synchronous, so no ongoing process to stop
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        try
        {
            await InitializeAsync();

            return new CredentialsValidationResult
            {
                IsValid = _tts != null,
                AvailableVoiceCount = _tts != null ? 1 : 0,
                EngineName = "sherpa-onnx"
            };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                EngineName = "sherpa-onnx"
            };
        }
    }

    private async IAsyncEnumerable<AudioChunkEventArgs> CreateAudioChunkStream(byte[] audioData, AudioFormat format)
    {
        const int chunkSize = 4096; // 4KB chunks
        var position = 0;

        while (position < audioData.Length)
        {
            var remainingBytes = audioData.Length - position;
            var chunkSizeToUse = Math.Min(chunkSize, remainingBytes);

            var chunk = new byte[chunkSizeToUse];
            Array.Copy(audioData, position, chunk, 0, chunkSizeToUse);

            yield return new AudioChunkEventArgs
            {
                AudioData = chunk,
                Format = format,
                IsFinal = (position + chunkSizeToUse) >= audioData.Length,
                Position = position
            };

            position += chunkSizeToUse;
            await Task.Delay(50); // Small delay to simulate streaming
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stop();
            _tts = null;
            _isInitialized = false;
        }
        base.Dispose(disposing);
    }

    private class ModelConfiguration
    {
        public string ModelType { get; set; } = "vits";
        public string ModelPath { get; set; } = string.Empty;
        public string TokensPath { get; set; } = string.Empty;
        public string DataDir { get; set; } = string.Empty;
        public string LexiconPath { get; set; } = string.Empty;
        public string VoicesPath { get; set; } = string.Empty;
        public string VocoderPath { get; set; } = string.Empty;
        public string RuleFsts { get; set; } = string.Empty;
        public string RuleFars { get; set; } = string.Empty;
        public string? Url { get; set; }
    }

    /// <summary>
    /// SherpaOnnx model configuration from JSON
    /// </summary>
    private class SherpaOnnxModelConfig
    {
        public string? Name { get; set; }
        public string? ModelType { get; set; }
        public string? Developer { get; set; }
        public string? Gender { get; set; }
        public string? Url { get; set; }
        public int? SampleRate { get; set; }
        public List<SherpaOnnxLanguage>? Language { get; set; }
    }

    /// <summary>
    /// SherpaOnnx language configuration
    /// </summary>
    private class SherpaOnnxLanguage
    {
        public string? LangCode { get; set; }
        public string? LanguageName { get; set; }
        public string? Country { get; set; }
    }
}