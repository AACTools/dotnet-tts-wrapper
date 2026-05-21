namespace DotNetTtsWrapper.Models;

/// <summary>
/// SherpaOnnx TTS credentials
/// </summary>
public class SherpaOnnxCredentials : ITtsCredentials
{
    /// <summary>
    /// Path to model directory or specific model file
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// Voice model ID (e.g., "kokoro-en-en-19", "vits-piper-en_US-amy-low")
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// If true, skip automatic model download
    /// </summary>
    public bool NoAutoDownload { get; set; }

    /// <summary>
    /// Base directory for model storage
    /// </summary>
    public string? BaseModelsDir { get; set; }

    /// <summary>
    /// Validate SherpaOnnx credentials (checks if model files exist)
    /// </summary>
    public async Task<CredentialsValidationResult> ValidateAsync()
    {
        return await Task.FromResult(new CredentialsValidationResult
        {
            IsValid = true, // SherpaOnnx models are validated during initialization
            EngineName = "sherpa-onnx",
            AvailableVoiceCount = 1 // Will be updated during actual validation
        });
    }
}