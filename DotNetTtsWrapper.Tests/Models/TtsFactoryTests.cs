using DotNetTtsWrapper.Engines;
using DotNetTtsWrapper.Models;
using DotNetTtsWrapper.Tests.TestHelpers;
using Xunit.Abstractions;

namespace DotNetTtsWrapper.Tests.Models;

/// <summary>
/// Tests for TTS Factory
/// </summary>
public class TtsFactoryTests : IDisposable
{
    private readonly ITestOutputHelper _output;

    public TtsFactoryTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CreateClient_Sapi_ShouldReturnSapiClient()
    {
        var client = TtsFactory.CreateClient("sapi");
        Assert.NotNull(client);
        Assert.IsType<SapiTtsClient>(client);
    }

    [Fact]
    public void CreateClient_SherpaOnnx_ShouldReturnSherpaOnnxClient()
    {
        var credentials = new SherpaOnnxCredentials { NoAutoDownload = true };
        var client = TtsFactory.CreateClient("sherpaonnx", credentials);
        Assert.NotNull(client);
        Assert.IsType<SherpaOnnxTtsClient>(client);
    }

    [Fact]
    public void CreateClient_Azure_ShouldReturnAzureClient()
    {
        if (!CredentialsHelper.HasAzureCredentials())
        {
            _output.WriteLine("Azure credentials not available");
            return;
        }

        var credentials = CredentialsHelper.GetAzureCredentials();
        var client = TtsFactory.CreateClient("azure", credentials);
        Assert.NotNull(client);
        Assert.IsType<AzureSdkTtsClient>(client);
    }

    [Fact]
    public void CreateClient_Polly_ShouldReturnPollyClient()
    {
        if (!CredentialsHelper.HasPollyCredentials())
        {
            _output.WriteLine("Polly credentials not available");
            return;
        }

        var credentials = CredentialsHelper.GetPollyCredentials();
        var client = TtsFactory.CreateClient("polly", credentials);
        Assert.NotNull(client);
        Assert.IsType<PollyTtsClient>(client);
    }

    [Fact]
    public void CreateClient_ElevenLabs_ShouldReturnElevenLabsClient()
    {
        if (!CredentialsHelper.HasElevenLabsCredentials())
        {
            _output.WriteLine("ElevenLabs credentials not available");
            return;
        }

        var credentials = CredentialsHelper.GetElevenLabsCredentials();
        var client = TtsFactory.CreateClient("elevenlabs", credentials);
        Assert.NotNull(client);
        Assert.IsType<ElevenLabsTtsClient>(client);
    }

    [Fact]
    public void CreateClient_InvalidEngine_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() => TtsFactory.CreateClient("invalid-engine"));
    }

    [Fact]
    public void GetSupportedEngines_ShouldReturnAllEngines()
    {
        var engines = TtsFactory.GetSupportedEngines();

        Assert.NotNull(engines);
        Assert.NotEmpty(engines);

        var expectedEngines = new[] { "sapi", "azure", "sherpaonnx", "google", "polly", "openai", "elevenlabs" };

        foreach (var expected in expectedEngines)
        {
            Assert.Contains(expected, engines);
            _output.WriteLine($"✓ Engine '{expected}' is supported");
        }

        _output.WriteLine($"Total supported engines: {engines.Count()}");
    }

    [Fact]
    public void CreateClient_CaseInsensitive_ShouldWork()
    {
        var client1 = TtsFactory.CreateClient("SAPI");
        var client2 = TtsFactory.CreateClient("Sapi");

        Assert.NotNull(client1);
        Assert.NotNull(client2);

        Assert.IsType<SapiTtsClient>(client1);
        Assert.IsType<SapiTtsClient>(client2);
    }

    public void Dispose()
    {
        // Clean up any resources
    }
}