using DotNetTtsWrapper.Engines;
using DotNetTtsWrapper.Models;
using DotNetTtsWrapper.Tests.TestHelpers;
using Xunit.Abstractions;

namespace DotNetTtsWrapper.Tests.Engines;

/// <summary>
/// Tests for AWS Polly TTS client
/// </summary>
[Collection("Polly Tests")]
public class PollyTtsClientTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly PollyTtsClient? _client;

    public PollyTtsClientTests(ITestOutputHelper output)
    {
        _output = output;

        if (CredentialsHelper.HasPollyCredentials())
        {
            try
            {
                var credentials = CredentialsHelper.GetPollyCredentials();
                _client = new PollyTtsClient(credentials);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Could not initialize Polly client: {ex.Message}");
            }
        }
        else
        {
            _output.WriteLine("Polly credentials not found in environment variables");
        }
    }

    [Fact]
    public async Task GetVoicesAsync_ShouldReturnVoices()
    {
        if (_client == null)
        {
            _output.WriteLine("Polly client not available");
            return;
        }

        var voices = await _client.GetVoicesAsync();

        Assert.NotNull(voices);
        Assert.NotEmpty(voices);

        _output.WriteLine($"Found {voices.Count} Polly voices");

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
            _output.WriteLine("Polly client not available");
            return;
        }

        var testText = "Hello, this is a test of the AWS Polly text to speech engine.";

        var result = await _client.SynthToBytesAsync(testText);

        Assert.NotNull(result);
        Assert.NotNull(result.AudioData);
        Assert.True(result.AudioData.Length > 0, "Audio data should not be empty");

        _output.WriteLine($"Generated {result.AudioData.Length} bytes of audio");
        _output.WriteLine($"Format: {result.Format}, Sample Rate: {result.SampleRate}, Channels: {result.Channels}");
    }

    [Fact]
    public async Task SynthToFileAsync_ShouldCreateFile()
    {
        if (_client == null)
        {
            _output.WriteLine("Polly client not available");
            return;
        }

        var testText = "Testing Polly file output.";
        var tempFile = Path.Combine(Path.GetTempPath(), $"polly_test_{Guid.NewGuid()}.mp3");

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
    public async Task CheckCredentialsAsync_ShouldValidateSuccessfully()
    {
        if (_client == null)
        {
            _output.WriteLine("Polly client not available");
            return;
        }

        var result = await _client.CheckCredentialsAsync();

        Assert.NotNull(result);
        Assert.True(result.IsValid, "Polly credentials should be valid");
        Assert.Equal("polly", result.EngineName);

        _output.WriteLine($"Credentials validation: IsValid={result.IsValid}, VoiceCount={result.AvailableVoiceCount}");
    }

    [Fact]
    public async Task GetVoicesByLanguageAsync_ShouldFilterByLanguage()
    {
        if (_client == null)
        {
            _output.WriteLine("Polly client not available");
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