using DotNetTtsWrapper.Engines;
using DotNetTtsWrapper.Models;
using DotNetTtsWrapper.Tests.TestHelpers;
using Xunit.Abstractions;

namespace DotNetTtsWrapper.Tests.Engines;

/// <summary>
/// Tests for Wit.ai TTS client
/// </summary>
[Collection("WitAI Tests")]
public class WitAiTtsClientTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly WitAiTtsClient? _client;

    public WitAiTtsClientTests(ITestOutputHelper output)
    {
        _output = output;

        if (CredentialsHelper.HasWitAiCredentials())
        {
            try
            {
                var credentials = CredentialsHelper.GetWitAiCredentials();
                _client = new WitAiTtsClient(credentials);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Could not initialize Wit.ai client: {ex.Message}");
            }
        }
        else
        {
            _output.WriteLine("Wit.ai credentials not found in environment variables");
        }
    }

    [Fact]
    public async Task SynthToBytesAsync_ShouldGenerateAudio()
    {
        if (_client == null)
        {
            _output.WriteLine("Wit.ai client not available");
            return;
        }

        var testText = "Hello, this is a test of the Wit.ai text to speech engine.";

        var result = await _client.SynthToBytesAsync(testText);

        Assert.NotNull(result);
        Assert.NotNull(result.AudioData);
        Assert.True(result.AudioData.Length > 0, "Audio data should not be empty");

        _output.WriteLine($"Generated {result.AudioData.Length} bytes of audio");
    }

    [Fact]
    public async Task CheckCredentialsAsync_ShouldValidateSuccessfully()
    {
        if (_client == null)
        {
            _output.WriteLine("Wit.ai client not available");
            return;
        }

        var result = await _client.CheckCredentialsAsync();

        Assert.NotNull(result);
        Assert.True(result.IsValid, "Wit.ai credentials should be valid");
        Assert.Equal("witai", result.EngineName);

        _output.WriteLine($"Credentials validation: IsValid={result.IsValid}");
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}