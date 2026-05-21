namespace DotNetTtsWrapper.Models;

/// <summary>
/// Base credentials for cloud-based TTS services with API keys
/// </summary>
public abstract class ApiKeyCredentials : ITtsCredentials
{
    public string ApiKey { get; set; } = string.Empty;

    public async Task<CredentialsValidationResult> ValidateAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = "API key is required"
            };
        }

        return await ValidateApiKeyAsync();
    }

    protected abstract Task<CredentialsValidationResult> ValidateApiKeyAsync();
}

/// <summary>
/// Azure TTS credentials
/// </summary>
public class AzureCredentials : ApiKeyCredentials
{
    public string SubscriptionKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        // Basic validation - actual validation will happen in the client
        if (string.IsNullOrWhiteSpace(SubscriptionKey))
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = "Subscription key is required"
            };
        }

        if (string.IsNullOrWhiteSpace(Region))
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = "Region is required"
            };
        }

        return new CredentialsValidationResult
        {
            IsValid = true,
            EngineName = "azure"
        };
    }
}

/// <summary>
/// Google TTS credentials
/// </summary>
public class GoogleCredentials : ApiKeyCredentials
{
    public string? KeyFilePath { get; set; }

    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        if (!string.IsNullOrWhiteSpace(KeyFilePath) && !File.Exists(KeyFilePath))
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Key file not found: {KeyFilePath}"
            };
        }

        return new CredentialsValidationResult
        {
            IsValid = true,
            EngineName = "google"
        };
    }
}

/// <summary>
/// AWS Polly credentials
/// </summary>
public class PollyCredentials : ITtsCredentials
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";

    public async Task<CredentialsValidationResult> ValidateAsync()
    {
        if (string.IsNullOrWhiteSpace(AccessKeyId))
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = "Access Key ID is required"
            };
        }

        if (string.IsNullOrWhiteSpace(SecretAccessKey))
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = "Secret Access Key is required"
            };
        }

        return new CredentialsValidationResult
        {
            IsValid = true,
            EngineName = "polly"
        };
    }
}

/// <summary>
/// OpenAI TTS credentials
/// </summary>
public class OpenAICredentials : ApiKeyCredentials
{
    public string OrganizationId { get; set; } = string.Empty;

    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult
        {
            IsValid = !string.IsNullOrWhiteSpace(ApiKey),
            EngineName = "openai"
        };
    }
}

/// <summary>
/// ElevenLabs TTS credentials
/// </summary>
public class ElevenLabsCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult
        {
            IsValid = !string.IsNullOrWhiteSpace(ApiKey),
            EngineName = "elevenlabs"
        };
    }
}

/// <summary>
/// IBM Watson credentials
/// </summary>
public class WatsonCredentials : ApiKeyCredentials
{
    public string ServiceUrl { get; set; } = string.Empty;

    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(ServiceUrl))
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = "Service URL is required"
            };
        }

        return new CredentialsValidationResult
        {
            IsValid = !string.IsNullOrWhiteSpace(ApiKey),
            EngineName = "watson"
        };
    }
}

/// <summary>
/// PlayHT credentials
/// </summary>
public class PlayHtCredentials : ITtsCredentials
{
    public string ApiKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    public async Task<CredentialsValidationResult> ValidateAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey) || string.IsNullOrWhiteSpace(UserId))
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = "Both API Key and User ID are required"
            };
        }

        return new CredentialsValidationResult
        {
            IsValid = true,
            EngineName = "playht"
        };
    }
}

/// <summary>
/// Wit.ai credentials
/// </summary>
public class WitAiCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult
        {
            IsValid = !string.IsNullOrWhiteSpace(ApiKey),
            EngineName = "witai"
        };
    }
}

// Placeholder credentials for other services - these will be expanded
public class GeminiCredentials : ApiKeyCredentials
{
    public string Model { get; set; } = "gemini-3.1-flash-tts-preview";

    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "gemini" };
    }
}

public class CartesiaCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "cartesia" };
    }
}

public class DeepgramCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "deepgram" };
    }
}

public class HumeCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "hume" };
    }
}

public class XaiCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "xai" };
    }
}

public class FishAudioCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "fishaudio" };
    }
}

public class MistralCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "mistral" };
    }
}

public class MurfCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "murf" };
    }
}

public class UnrealSpeechCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "unrealspeech" };
    }
}

public class ResembleCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "resemble" };
    }
}

public class UpliftAiCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "upliftai" };
    }
}

public class ModelsLabCredentials : ApiKeyCredentials
{
    protected override async Task<CredentialsValidationResult> ValidateApiKeyAsync()
    {
        return new CredentialsValidationResult { IsValid = !string.IsNullOrWhiteSpace(ApiKey), EngineName = "modelslab" };
    }
}