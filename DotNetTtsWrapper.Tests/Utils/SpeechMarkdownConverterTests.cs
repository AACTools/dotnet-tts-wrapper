using DotNetTtsWrapper.Events;
using DotNetTtsWrapper.Models;
using DotNetTtsWrapper.Utils;

namespace DotNetTtsWrapper.Tests.Utils;

public class SpeechMarkdownConverterTests : IDisposable
{
    private readonly SpeechMarkdownConverter _converter = new();

    [Fact]
    public void IsSpeechMarkdown_EmphasisStrong_ReturnsTrue()
    {
        Assert.True(_converter.IsSpeechMarkdown("Hello ++world++"));
    }

    [Fact]
    public void IsSpeechMarkdown_EmphasisModerate_ReturnsTrue()
    {
        Assert.True(_converter.IsSpeechMarkdown("Hello +world+"));
    }

    [Fact]
    public void IsSpeechMarkdown_Break_ReturnsTrue()
    {
        Assert.True(_converter.IsSpeechMarkdown("Hello [500ms] world"));
    }

    [Fact]
    public void IsSpeechMarkdown_RateModifier_ReturnsTrue()
    {
        Assert.True(_converter.IsSpeechMarkdown("(Hello)[rate:\"slow\"]"));
    }

    [Fact]
    public void IsSpeechMarkdown_PitchModifier_ReturnsTrue()
    {
        Assert.True(_converter.IsSpeechMarkdown("(Hello)[pitch:\"high\"]"));
    }

    [Fact]
    public void IsSpeechMarkdown_PlainText_ReturnsFalse()
    {
        Assert.False(_converter.IsSpeechMarkdown("Hello world"));
    }

    [Fact]
    public void IsSpeechMarkdown_EmptyString_ReturnsFalse()
    {
        Assert.False(_converter.IsSpeechMarkdown(""));
    }

    [Fact]
    public void IsSpeechMarkdown_Ssml_ReturnsFalse()
    {
        Assert.False(_converter.IsSpeechMarkdown("<speak>Hello world</speak>"));
    }

    [Fact]
    public void ToSsml_PlainText_WrapsInSpeak()
    {
        var ssml = _converter.ToSsml("Hello world", global::SpeechMarkdown.Platform.W3c);
        Assert.Contains("<speak>", ssml);
        Assert.Contains("Hello world", ssml);
        Assert.Contains("</speak>", ssml);
    }

    [Fact]
    public void ToSsml_Emphasis_ConvertsCorrectly()
    {
        var ssml = _converter.ToSsml("Hello ++world++", global::SpeechMarkdown.Platform.AmazonAlexa);
        Assert.Contains("<speak>", ssml);
        Assert.Contains("<emphasis", ssml);
        Assert.Contains("world", ssml);
    }

    [Fact]
    public void ToSsml_Break_ConvertsCorrectly()
    {
        var ssml = _converter.ToSsml("Hello [500ms] world", global::SpeechMarkdown.Platform.AmazonAlexa);
        Assert.Contains("<speak>", ssml);
        Assert.Contains("<break", ssml);
    }

    [Fact]
    public void ToSsml_RateModifier_ConvertsCorrectly()
    {
        var ssml = _converter.ToSsml("(Hello)[rate:\"slow\"]", global::SpeechMarkdown.Platform.AmazonAlexa);
        Assert.Contains("<speak>", ssml);
        Assert.Contains("prosody", ssml);
    }

    [Fact]
    public void ToText_PlainText_ReturnsSame()
    {
        var text = _converter.ToText("Hello world");
        Assert.Equal("Hello world", text);
    }

    [Fact]
    public void ToText_Emphasis_StripsMarkup()
    {
        var text = _converter.ToText("Hello ++world++");
        Assert.Contains("Hello", text);
        Assert.Contains("world", text);
        Assert.DoesNotContain("++", text);
    }

    [Fact]
    public void ToText_Break_StripsMarkup()
    {
        var text = _converter.ToText("Hello [500ms] world");
        Assert.Contains("Hello", text);
        Assert.Contains("world", text);
        Assert.DoesNotContain("[500ms]", text);
    }

    [Fact]
    public void Validate_PlainText_ReturnsTrue()
    {
        Assert.True(_converter.Validate("Hello world"));
    }

    [Fact]
    public void Validate_ValidMarkdown_ReturnsTrue()
    {
        Assert.True(_converter.Validate("Hello ++world++"));
    }

    public void Dispose()
    {
        _converter.Dispose();
    }
}
