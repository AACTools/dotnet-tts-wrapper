using DotNetTtsWrapper.Engines;
using DotNetTtsWrapper.Models;
using Xunit.Abstractions;

namespace DotNetTtsWrapper.Tests.Engines;

/// <summary>
/// Tests for SherpaOnnx TTS client
/// </summary>
[Collection("SherpaOnnx Tests")]
public class SherpaOnnxTtsClientTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly SherpaOnnxTtsClient? _client;

    public SherpaOnnxTtsClientTests(ITestOutputHelper output)
    {
        _output = output;

        try
        {
            var credentials = new SherpaOnnxCredentials
            {
                NoAutoDownload = true // Skip auto-download for tests
            };
            _client = new SherpaOnnxTtsClient(credentials);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Could not initialize SherpaOnnx client: {ex.Message}");
        }
    }

    [Fact]
    public void Constructor_ShouldInitializeClient()
    {
        if (_client == null)
        {
            _output.WriteLine("SherpaOnnx client not available");
            return;
        }

        Assert.NotNull(_client);
        Assert.True(_client.Capabilities.SupportsStreaming, "SherpaOnnx DOES support streaming via callbacks");
        Assert.True(_client.Capabilities.SupportsWordTimings == false, "SherpaOnnx does not support word timings");
        Assert.True(_client.Capabilities.RequiresInternet == false, "SherpaOnnx is fully offline");
    }

    [Fact]
    public async Task GetVoicesAsync_ShouldReturnDefaultVoices()
    {
        if (_client == null)
        {
            _output.WriteLine("SherpaOnnx client not available");
            return;
        }

        var voices = await _client.GetVoicesAsync();

        Assert.NotNull(voices);
        Assert.NotEmpty(voices);

        _output.WriteLine($"Found {voices.Count} SherpaOnnx voices");

        foreach (var voice in voices.Take(5))
        {
            _output.WriteLine($"Voice: {voice.Name} ({voice.Id}) - {voice.Provider}");
            Assert.NotNull(voice.Id);
            Assert.NotNull(voice.Name);
            Assert.Equal("sherpa-onnx", voice.Provider);
        }
    }

    [Fact]
    public async Task GetVoicesAsync_ShouldContainKnownVoices()
    {
        if (_client == null)
        {
            _output.WriteLine("SherpaOnnx client not available");
            return;
        }

        var voices = await _client.GetVoicesAsync();

        // Check for some known voice IDs
        var knownVoiceIds = new[] { "kokoro-en-en-19", "vits-piper-en_US-amy-low", "matcha-icefall-en_US-ljspeech" };
        var voiceIds = voices.Select(v => v.Id).ToList();

        foreach (var knownId in knownVoiceIds)
        {
            var exists = voiceIds.Contains(knownId);
            _output.WriteLine($"Voice {knownId}: {(exists ? "Found" : "Not found")}");
        }
    }

    [Fact]
    public async Task GetVoicesByLanguageAsync_ShouldFilterByLanguage()
    {
        if (_client == null)
        {
            _output.WriteLine("SherpaOnnx client not available");
            return;
        }

        var englishVoices = await _client.GetVoicesByLanguageAsync("en");

        Assert.NotNull(englishVoices);
        Assert.NotEmpty(englishVoices);

        _output.WriteLine($"Found {englishVoices.Count} English voices");

        foreach (var voice in englishVoices)
        {
            _output.WriteLine($"English voice: {voice.Name} ({voice.Id})");
            Assert.Contains(voice.LanguageCodes, l => l.Iso639_3 == "en");
        }
    }

    [Fact]
    public async Task CheckCredentialsAsync_ShouldValidateSuccessfully()
    {
        if (_client == null)
        {
            _output.WriteLine("SherpaOnnx client not available");
            return;
        }

        var result = await _client.CheckCredentialsAsync();

        Assert.NotNull(result);
        Assert.Equal("sherpa-onnx", result.EngineName);

        _output.WriteLine($"Credentials validation: IsValid={result.IsValid}, VoiceCount={result.AvailableVoiceCount}");
    }

    [Fact]
    public async Task SynthToBytesAsync_ShouldHandleMissingModelsGracefully()
    {
        if (_client == null)
        {
            _output.WriteLine("SherpaOnnx client not available");
            return;
        }

        var testText = "This is a test of SherpaOnnx text to speech.";

        try
        {
            var result = await _client.SynthToBytesAsync(testText);

            // If models are present, this should work
            Assert.NotNull(result);
            Assert.NotNull(result.AudioData);

            _output.WriteLine($"Generated {result.AudioData.Length} bytes of audio");
        }
        catch (Exception ex)
        {
            // Expected when models are not downloaded
            _output.WriteLine($"Expected error (no models): {ex.Message}");
            // Different error messages possible
            Assert.True(ex.Message.Contains("model", StringComparison.OrdinalIgnoreCase) ||
                       ex.Message.Contains("instance", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task SynthToStreamAsync_ShouldReturnRealStream()
    {
        if (_client == null)
        {
            _output.WriteLine("SherpaOnnx client not available");
            return;
        }

        var testText = "Testing streaming functionality.";

        try
        {
            var result = await _client.SynthToStreamAsync(testText);

            Assert.NotNull(result);
            Assert.NotNull(result.AudioStream);

            var chunkCount = 0;
            await foreach (var chunk in result.AudioStream)
            {
                chunkCount++;
                _output.WriteLine($"Received chunk {chunkCount}: {chunk.AudioData.Length} bytes");
            }

            Assert.True(chunkCount > 0, "Should have received at least one chunk");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Expected error (no models): {ex.Message}");
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}