using DotNetTtsWrapper.Engines;
using DotNetTtsWrapper.Models;
using DotNetTtsWrapper.Tests.TestHelpers;
using Xunit.Abstractions;

namespace DotNetTtsWrapper.Tests.Engines;

/// <summary>
/// Tests for Azure Speech SDK TTS client
/// </summary>
[Collection("Azure Tests")]
public class AzureTtsClientTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly AzureSdkTtsClient? _client;

    public AzureTtsClientTests(ITestOutputHelper output)
    {
        _output = output;

        if (CredentialsHelper.HasAzureCredentials())
        {
            try
            {
                var credentials = CredentialsHelper.GetAzureCredentials();
                _client = new AzureSdkTtsClient(credentials);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Could not initialize Azure client: {ex.Message}");
            }
        }
        else
        {
            _output.WriteLine("Azure credentials not found in environment variables");
        }
    }

    [Fact]
    public async Task GetVoicesAsync_ShouldReturnVoices()
    {
        if (_client == null)
        {
            _output.WriteLine("Azure client not available");
            return;
        }

        var voices = await _client.GetVoicesAsync();

        Assert.NotNull(voices);
        Assert.NotEmpty(voices);

        _output.WriteLine($"Found {voices.Count} Azure voices");

        foreach (var voice in voices.Take(5))
        {
            _output.WriteLine($"Voice: {voice.Name} ({voice.Id}) - {voice.Gender}");
            Assert.NotNull(voice.Id);
            Assert.NotNull(voice.Name);
        }
    }

    [Fact]
    public async Task SynthToBytesAsync_ShouldGenerateAudio()
    {
        if (_client == null)
        {
            _output.WriteLine("Azure client not available");
            return;
        }

        var testText = "Hello, this is a test of the Azure text to speech engine.";

        var result = await _client.SynthToBytesAsync(testText);

        Assert.NotNull(result);
        Assert.NotNull(result.AudioData);
        Assert.True(result.AudioData.Length > 0, "Audio data should not be empty");
        Assert.Equal(AudioFormat.Mp3, result.Format);

        _output.WriteLine($"Generated {result.AudioData.Length} bytes of audio");
        _output.WriteLine($"Format: {result.Format}, Sample Rate: {result.SampleRate}, Channels: {result.Channels}");
    }

    [Fact]
    public async Task SynthToBytesAsync_WithWordTimings_ShouldReturnTimings()
    {
        if (_client == null)
        {
            _output.WriteLine("Azure client not available");
            return;
        }

        var testText = "Testing word boundaries with Azure Speech SDK.";

        var options = new TtsOptions { EnableWordTimings = true };
        var result = await _client.SynthToBytesAsync(testText, options);

        Assert.NotNull(result);
        Assert.NotNull(result.AudioData);
        Assert.True(result.AudioData.Length > 0);
        Assert.NotNull(result.WordTimings);

        _output.WriteLine($"Generated {result.AudioData.Length} bytes with {result.WordTimings.Count} word timings");

        foreach (var timing in result.WordTimings)
        {
            _output.WriteLine($"Word: '{timing.Text}' ({timing.StartTime:F3}s - {timing.EndTime:F3}s)");
        }
    }

    [Fact]
    public async Task SynthToFileAsync_ShouldCreateFile()
    {
        if (_client == null)
        {
            _output.WriteLine("Azure client not available");
            return;
        }

        var testText = "Testing Azure file output.";
        var tempFile = Path.Combine(Path.GetTempPath(), $"azure_test_{Guid.NewGuid()}.mp3");

        try
        {
            await _client.SynthToFileAsync(testText, tempFile, AudioFormat.Mp3);

            Assert.True(File.Exists(tempFile), "Output file should exist");

            var fileInfo = new FileInfo(tempFile);
            Assert.True(fileInfo.Length > 0, "Output file should not be empty");

            _output.WriteLine($"Created file: {tempFile} ({fileInfo.Length} bytes)");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SynthToStreamAsync_ShouldStreamAudio()
    {
        if (_client == null)
        {
            _output.WriteLine("Azure client not available");
            return;
        }

        var testText = "Testing Azure streaming functionality.";

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

    [Fact]
    public async Task CheckCredentialsAsync_ShouldValidateSuccessfully()
    {
        if (_client == null)
        {
            _output.WriteLine("Azure client not available");
            return;
        }

        var result = await _client.CheckCredentialsAsync();

        Assert.NotNull(result);
        Assert.True(result.IsValid, "Azure credentials should be valid");
        Assert.Equal("azure-sdk", result.EngineName);

        _output.WriteLine($"Credentials validation: IsValid={result.IsValid}, VoiceCount={result.AvailableVoiceCount}");
    }

    [Fact]
    public async Task GetVoicesByLanguageAsync_ShouldFilterByLanguage()
    {
        if (_client == null)
        {
            _output.WriteLine("Azure client not available");
            return;
        }

        var englishVoices = await _client.GetVoicesByLanguageAsync("en");

        Assert.NotNull(englishVoices);
        Assert.NotEmpty(englishVoices);

        _output.WriteLine($"Found {englishVoices.Count} English voices");

        foreach (var voice in englishVoices.Take(3))
        {
            _output.WriteLine($"English voice: {voice.Name} ({voice.Id})");
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}