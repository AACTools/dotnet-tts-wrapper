using DotNetTtsWrapper.Engines;
using DotNetTtsWrapper.Models;
using DotNetTtsWrapper.Tests.TestHelpers;
using Xunit.Abstractions;

namespace DotNetTtsWrapper.Tests.Engines;

/// <summary>
/// Tests for Play.ht TTS client
/// </summary>
[Collection("PlayHT Tests")]
public class PlayHtTtsClientTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly PlayHtTtsClient? _client;

    public PlayHtTtsClientTests(ITestOutputHelper output)
    {
        _output = output;

        if (CredentialsHelper.HasPlayHtCredentials())
        {
            try
            {
                var credentials = CredentialsHelper.GetPlayHtCredentials();
                _client = new PlayHtTtsClient(credentials);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Could not initialize Play.ht client: {ex.Message}");
            }
        }
        else
        {
            _output.WriteLine("Play.ht credentials not found in environment variables");
        }
    }

    [Fact]
    public async Task GetVoicesAsync_ShouldReturnVoices()
    {
        if (_client == null)
        {
            _output.WriteLine("Play.ht client not available");
            return;
        }

        var voices = await _client.GetVoicesAsync();

        Assert.NotNull(voices);
        Assert.NotEmpty(voices);

        _output.WriteLine($"Found {voices.Count} Play.ht voices");

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
            _output.WriteLine("Play.ht client not available");
            return;
        }

        var testText = "Hello, this is a test of the Play.ht text to speech engine.";

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
            _output.WriteLine("Play.ht client not available");
            return;
        }

        var result = await _client.CheckCredentialsAsync();

        Assert.NotNull(result);
        Assert.True(result.IsValid, "Play.ht credentials should be valid");
        Assert.Equal("playht", result.EngineName);

        _output.WriteLine($"Credentials validation: IsValid={result.IsValid}");
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}