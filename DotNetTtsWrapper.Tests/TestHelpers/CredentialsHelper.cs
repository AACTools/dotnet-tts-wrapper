using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Tests.TestHelpers;

/// <summary>
/// Helper class for getting test credentials from environment variables
/// </summary>
public static class CredentialsHelper
{
    public static AzureCredentials GetAzureCredentials()
    {
        var token = Environment.GetEnvironmentVariable("MICROSOFT_TOKEN");
        var region = Environment.GetEnvironmentVariable("MICROSOFT_REGION");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(region))
            throw new InvalidOperationException("Azure credentials not found in environment variables");

        return new AzureCredentials
        {
            SubscriptionKey = token,
            Region = region
        };
    }

    public static PollyCredentials GetPollyCredentials()
    {
        var keyId = Environment.GetEnvironmentVariable("POLLY_AWS_KEY_ID");
        var accessKey = Environment.GetEnvironmentVariable("POLLY_AWS_ACCESS_KEY");
        var region = Environment.GetEnvironmentVariable("POLLY_REGION");

        if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(region))
            throw new InvalidOperationException("Polly credentials not found in environment variables");

        return new PollyCredentials
        {
            AccessKeyId = keyId,
            SecretAccessKey = accessKey,
            Region = region
        };
    }

    public static ElevenLabsCredentials GetElevenLabsCredentials()
    {
        var apiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("ElevenLabs credentials not found in environment variables");

        return new ElevenLabsCredentials
        {
            ApiKey = apiKey
        };
    }

    public static WitAiCredentials GetWitAiCredentials()
    {
        var token = Environment.GetEnvironmentVariable("WITAI_TOKEN");

        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("Wit.ai credentials not found in environment variables");

        return new WitAiCredentials
        {
            ApiKey = token
        };
    }

    public static PlayHtCredentials GetPlayHtCredentials()
    {
        var apiKey = Environment.GetEnvironmentVariable("PLAYHT_API_KEY");
        var userId = Environment.GetEnvironmentVariable("PLAYHT_USER_ID");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("Play.ht credentials not found in environment variables");

        return new PlayHtCredentials
        {
            ApiKey = apiKey,
            UserId = userId
        };
    }

    public static bool HasAzureCredentials() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MICROSOFT_TOKEN")) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MICROSOFT_REGION"));

    public static bool HasPollyCredentials() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POLLY_AWS_KEY_ID")) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POLLY_AWS_ACCESS_KEY")) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POLLY_REGION"));

    public static bool HasElevenLabsCredentials() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY"));

    public static bool HasWitAiCredentials() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WITAI_TOKEN"));

    public static bool HasPlayHtCredentials() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PLAYHT_API_KEY")) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PLAYHT_USER_ID"));
}