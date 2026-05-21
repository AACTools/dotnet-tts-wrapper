using DotNetTtsWrapper.Engines;
using DotNetTtsWrapper.Models;
using DotNetTtsWrapper.Tests.TestHelpers;
using Xunit.Abstractions;

namespace DotNetTtsWrapper.Tests.Engines;

/// <summary>
/// Tests for ElevenLabs TTS client
/// </summary>
[Collection("ElevenLabs Tests")]
public class ElevenLabsTtsClientTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ElevenLabsTtsClient? _client;

    public ElevenLabsTtsClientTests(ITestOutputHelper output)
    {
        _output = output;

        if (CredentialsHelper.HasElevenLabsCredentials())
        {
            try
            {
                var credentials = CredentialsHelper.GetElevenLabsCredentials();
                _client = new ElevenLabsTtsClient(credentials);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Could not initialize ElevenLabs client: {ex.Message}");
            }
        }
        else
        {
            _output.WriteLine("ElevenLabs credentials not found in environment variables");
        }
    }

    [Fact]
    public async Task GetVoicesAsync_ShouldReturnVoices()
    {
        if (_client == null)
        {
            _output.WriteLine("ElevenLabs client not available");
            return;
        }

        var voices = await _client.GetVoicesAsync();

        Assert.NotNull(voices);
        Assert.NotEmpty(voices);

        _output.WriteLine($"Found {voices.Count} ElevenLabs voices");

        foreach (var voice in voices.Take(5))
        {
            _output.WriteLine($"Voice: {voice.Name} ({voice.Id}) - {voice.Gender}");
        }
    }

    [Fact]
    public async Task SynthToBytesAsync_ShouldGenerateAudio()
    {
        if (_client == null)
        {
            _output.WriteLine("ElevenLabs client not available");
            return;
        }

        var testText = "Hello, this is a test of the ElevenLabs text to speech engine.";

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
            _output.WriteLine("ElevenLabs client not available");
            return;
        }

        var result = await _client.CheckCredentialsAsync();

        Assert.NotNull(result);
        Assert.True(result.IsValid, "ElevenLabs credentials should be valid");
        Assert.Equal("elevenlabs", result.EngineName);

        _output.WriteLine($"Credentials validation: IsValid={result.IsValid}");
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}