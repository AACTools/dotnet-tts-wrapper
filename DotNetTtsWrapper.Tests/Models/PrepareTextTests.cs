using DotNetTtsWrapper.Events;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Tests.Models;

public class TestTtsClient : AbstractTtsClient
{
    public string? LastPreparedText { get; private set; }

    public string TestPrepareText(string text, TtsOptions? options = null)
    {
        return PrepareTextAsync(text, options).GetAwaiter().GetResult();
    }

    public string TestCreateSsml(string text, TtsOptions? options = null)
    {
        return CreateSsml(text, options);
    }

    public new string TestGetSpeechMarkdownPlatform()
    {
        return GetSpeechMarkdownPlatform();
    }

    public TestTtsClient WithSsmlSupport()
    {
        Capabilities.SupportsSsml = true;
        Capabilities.SupportsSpeechMarkdown = true;
        return this;
    }

    public TestTtsClient WithoutSsmlSupport()
    {
        Capabilities.SupportsSsml = false;
        Capabilities.SupportsSpeechMarkdown = true;
        return this;
    }

    public override Task<List<TtsVoice>> GetVoicesAsync() => Task.FromResult(new List<TtsVoice>());
    public override Task<List<TtsVoice>> GetVoicesByLanguageAsync(string languageCode) => Task.FromResult(new List<TtsVoice>());
    public override Task<TtsSynthesisResult> SynthToBytesAsync(string text, TtsOptions? options = null) => Task.FromResult(new TtsSynthesisResult());
    public override Task<StreamingTtsResult> SynthToStreamAsync(string text, TtsOptions? options = null) => Task.FromResult(new StreamingTtsResult());
    public override Task SynthToFileAsync(string text, string outputPath, AudioFormat format = AudioFormat.Wav, TtsOptions? options = null) => Task.CompletedTask;
    public override Task SpeakAsync(string text, TtsOptions? options = null) => Task.CompletedTask;
    public override Task SpeakStreamedAsync(string text, Action<WordTimingEventArgs>? wordCallback = null, TtsOptions? options = null) => Task.CompletedTask;
    public override void Pause() { }
    public override void Resume() { }
    public override void Stop() { }
    public override Task<CredentialsValidationResult> CheckCredentialsAsync() => Task.FromResult(new CredentialsValidationResult { IsValid = true });
}

public class PrepareTextTests : IDisposable
{
    [Fact]
    public void PrepareText_PlainText_ReturnsAsIs()
    {
        using var client = new TestTtsClient();
        var result = client.TestPrepareText("Hello world");
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void PrepareText_Ssml_ReturnsAsIs()
    {
        using var client = new TestTtsClient();
        var result = client.TestPrepareText("<speak>Hello world</speak>");
        Assert.Equal("<speak>Hello world</speak>", result);
    }

    [Fact]
    public void PrepareText_SpeechMarkdown_WithSsmlSupport_ConvertsToSsml()
    {
        using var client = new TestTtsClient().WithSsmlSupport();
        var result = client.TestPrepareText("Hello ++world++");
        Assert.Contains("<speak>", result);
        Assert.Contains("world", result);
        Assert.Contains("<emphasis", result);
    }

    [Fact]
    public void PrepareText_SpeechMarkdown_WithoutSsmlSupport_ConvertsToPlainText()
    {
        using var client = new TestTtsClient().WithoutSsmlSupport();
        var result = client.TestPrepareText("Hello ++world++");
        Assert.Contains("Hello", result);
        Assert.Contains("world", result);
        Assert.DoesNotContain("<speak>", result);
        Assert.DoesNotContain("++", result);
    }

    [Fact]
    public void PrepareText_SpeechMarkdown_ExplicitlyEnabled_ConvertsToSsml()
    {
        using var client = new TestTtsClient().WithSsmlSupport();
        var result = client.TestPrepareText("Hello ++world++", new TtsOptions { UseSpeechMarkdown = true });
        Assert.Contains("<speak>", result);
        Assert.Contains("<emphasis", result);
    }

    [Fact]
    public void PrepareText_SpeechMarkdown_ExplicitlyDisabled_ReturnsAsIs()
    {
        using var client = new TestTtsClient().WithSsmlSupport();
        var result = client.TestPrepareText("Hello ++world++", new TtsOptions { UseSpeechMarkdown = false });
        Assert.Equal("Hello ++world++", result);
    }

    [Fact]
    public void PrepareText_RawSsml_PassesThrough()
    {
        using var client = new TestTtsClient().WithSsmlSupport();
        var ssml = "<speak>Raw SSML content</speak>";
        var result = client.TestPrepareText(ssml, new TtsOptions { RawSsml = true });
        Assert.Equal(ssml, result);
    }

    [Fact]
    public void PrepareText_EmptyText_Throws()
    {
        using var client = new TestTtsClient();
        Assert.Throws<ArgumentException>(() => client.TestPrepareText(""));
    }

    [Fact]
    public void PrepareText_BreakModifier_ConvertsToSsml()
    {
        using var client = new TestTtsClient().WithSsmlSupport();
        var result = client.TestPrepareText("Hello [500ms] world");
        Assert.Contains("<speak>", result);
        Assert.Contains("<break", result);
    }

    [Fact]
    public void PrepareText_RateModifier_ConvertsToSsml()
    {
        using var client = new TestTtsClient().WithSsmlSupport();
        var result = client.TestPrepareText("(Hello)[rate:\"slow\"]");
        Assert.Contains("<speak>", result);
        Assert.Contains("prosody", result);
    }

    public void Dispose() { }
}

public class SsmlBuilderTests
{
    [Fact]
    public void Build_PlainText_WrapsCorrectly()
    {
        using var client = new TestTtsClient();
        var ssml = client.TestCreateSsml("Hello world");
        Assert.Contains("<speak", ssml);
        Assert.Contains("Hello world", ssml);
        Assert.Contains("</speak>", ssml);
    }

    [Fact]
    public void Build_WithVoice_IncludesVoiceTag()
    {
        using var client = new TestTtsClient();
        client.SetVoice("test-voice");
        var ssml = client.TestCreateSsml("Hello");
        Assert.Contains("<voice name=\"test-voice\">", ssml);
        Assert.Contains("</voice>", ssml);
    }

    [Fact]
    public void Build_WithRate_IncludesProsodyTag()
    {
        using var client = new TestTtsClient();
        var ssml = client.TestCreateSsml("Hello", new TtsOptions { Rate = SpeechRate.Fast });
        Assert.Contains("<prosody", ssml);
        Assert.Contains("rate=\"fast\"", ssml);
        Assert.Contains("</prosody>", ssml);
    }

    [Fact]
    public void Build_WithPitch_IncludesProsodyTag()
    {
        using var client = new TestTtsClient();
        var ssml = client.TestCreateSsml("Hello", new TtsOptions { Pitch = SpeechPitch.High });
        Assert.Contains("<prosody", ssml);
        Assert.Contains("pitch=\"high\"", ssml);
        Assert.Contains("</prosody>", ssml);
    }

    [Fact]
    public void Build_WithRateAndPitch_CombinesInOneProsody()
    {
        using var client = new TestTtsClient();
        var ssml = client.TestCreateSsml("Hello", new TtsOptions { Rate = SpeechRate.Fast, Pitch = SpeechPitch.High });
        Assert.Contains("<prosody", ssml);
        Assert.Contains("rate=\"fast\"", ssml);
        Assert.Contains("pitch=\"high\"", ssml);
        Assert.Contains("</prosody>", ssml);
        var prosodyCount = ssml.Split("<prosody").Length - 1;
        Assert.Equal(1, prosodyCount);
    }

    [Fact]
    public void Build_TagsProperlyClosed()
    {
        using var client = new TestTtsClient();
        client.SetVoice("voice1");
        var ssml = client.TestCreateSsml("Hello", new TtsOptions { Rate = SpeechRate.Fast });

        var openVoice = ssml.Split("<voice").Length - 1;
        var closeVoice = ssml.Split("</voice>").Length - 1;
        Assert.Equal(openVoice, closeVoice);

        var openProsody = ssml.Split("<prosody").Length - 1;
        var closeProsody = ssml.Split("</prosody>").Length - 1;
        Assert.Equal(openProsody, closeProsody);

        var openSpeak = ssml.Split("<speak").Length - 1;
        var closeSpeak = ssml.Split("</speak>").Length - 1;
        Assert.Equal(openSpeak, closeSpeak);
    }
}
