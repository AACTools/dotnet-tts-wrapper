namespace DotNetTtsWrapper.Models;

/// <summary>
/// Base interface for TTS provider credentials
/// </summary>
public interface ITtsCredentials
{
    /// <summary>
    /// Validates the credentials
    /// </summary>
    Task<CredentialsValidationResult> ValidateAsync();
}

/// <summary>
/// Result of credential validation
/// </summary>
public class CredentialsValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public int? AvailableVoiceCount { get; set; }
    public string? EngineName { get; set; }
}