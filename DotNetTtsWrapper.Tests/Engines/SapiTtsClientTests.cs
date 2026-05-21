using DotNetTtsWrapper.Engines;
using DotNetTtsWrapper.Models;
using Xunit.Abstractions;

namespace DotNetTtsWrapper.Tests.Engines;

/// <summary>
/// Tests for SAPI TTS client (Windows only)
/// </summary>
[Collection("SAPI Tests")]
public class SapiTtsClientTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly SapiTtsClient? _client;

    public SapiTtsClientTests(ITestOutputHelper output)
    {
        _output = output;

        try
        {
            _client = new SapiTtsClient();
        }
        catch (PlatformNotSupportedException)
        {
            _output.WriteLine("SAPI is not supported on this platform (Windows only)");
        }
    }

    [Fact]
    public void Constructor_ShouldThrowOnNonWindowsPlatform()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            Assert.Throws<PlatformNotSupportedException>(() => new SapiTtsClient());
        }
        else
        {
            // Should not throw on Windows
            var client = new SapiTtsClient();
            Assert.NotNull(client);
        }
    }

    [Fact]
    public async Task GetVoicesAsync_ShouldReturnVoices()
    {
        if (_client == null)
        {
            _output.WriteLine("SAPI client not available (Windows only)");
            return;
        }

        var voices = await _client.GetVoicesAsync();

        Assert.NotNull(voices);
        Assert.NotEmpty(voices);

        _output.WriteLine($"Found {voices.Count} SAPI voices");

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
            _output.WriteLine("SAPI client not available (Windows only)");
            return;
        }

        var testText = "Hello, this is a test of the SAPI text to speech engine.";

        var result = await _client.SynthToBytesAsync(testText);

        Assert.NotNull(result);
        Assert.NotNull(result.AudioData);
        Assert.True(result.AudioData.Length > 0, "Audio data should not be empty");
        Assert.Equal(AudioFormat.Wav, result.Format);

        _output.WriteLine($"Generated {result.AudioData.Length} bytes of audio");
        _output.WriteLine($"Format: {result.Format}, Sample Rate: {result.SampleRate}, Channels: {result.Channels}");
    }

    [Fact]
    public async Task SynthToFileAsync_ShouldCreateFile()
    {
        if (_client == null)
        {
            _output.WriteLine("SAPI client not available (Windows only)");
            return;
        }

        var testText = "Testing SAPI file output.";
        var tempFile = Path.Combine(Path.GetTempPath(), $"sapi_test_{Guid.NewGuid()}.wav");

        try
        {
            await _client.SynthToFileAsync(testText, tempFile);

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
    public async Task GetVoicesByLanguageAsync_ShouldFilterByLanguage()
    {
        if (_client == null)
        {
            _output.WriteLine("SAPI client not available (Windows only)");
            return;
        }

        var englishVoices = await _client.GetVoicesByLanguageAsync("en");

        Assert.NotNull(englishVoices);
        Assert.NotEmpty(englishVoices);

        _output.WriteLine($"Found {englishVoices.Count} English voices");

        foreach (var voice in englishVoices)
        {
            _output.WriteLine($"English voice: {voice.Name} ({voice.Id})");
        }
    }

    [Fact]
    public async Task CheckCredentialsAsync_ShouldValidateSuccessfully()
    {
        if (_client == null)
        {
            _output.WriteLine("SAPI client not available (Windows only)");
            return;
        }

        var result = await _client.CheckCredentialsAsync();

        Assert.NotNull(result);
        Assert.True(result.IsValid, "SAPI should always be valid on Windows");
        Assert.Equal("sapi", result.EngineName);

        _output.WriteLine($"Credentials validation: IsValid={result.IsValid}, VoiceCount={result.AvailableVoiceCount}");
    }

    [Fact]
    public async Task SetVoice_ShouldChangeVoice()
    {
        if (_client == null)
        {
            _output.WriteLine("SAPI client not available (Windows only)");
            return;
        }

        var voices = await _client.GetVoicesAsync();
        if (voices.Count > 1)
        {
            var secondVoice = voices[1];
            _client.SetVoice(secondVoice.Id);

            var result = await _client.SynthToBytesAsync("Testing voice change.");
            Assert.NotNull(result);
            Assert.True(result.AudioData.Length > 0);

            _output.WriteLine($"Successfully changed to voice: {secondVoice.Name}");
        }
        else
        {
            _output.WriteLine("Only one voice available, skipping voice change test");
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}