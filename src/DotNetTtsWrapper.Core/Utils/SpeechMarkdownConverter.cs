using SpeechMarkdown;

namespace DotNetTtsWrapper.Utils;

public class SpeechMarkdownConverter : IDisposable
{
    private readonly SpeechMarkdownParser _parser = new();

    public bool IsSpeechMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.TrimStart().StartsWith("<speak", StringComparison.OrdinalIgnoreCase))
            return false;

        return _parser.IsSpeechMarkdown(text);
    }

    public bool Validate(string markdown)
    {
        return _parser.Validate(markdown);
    }

    public string ToSsml(string markdown, string platform)
    {
        return _parser.ToSsml(markdown, platform);
    }

    public string ToText(string markdown)
    {
        return _parser.ToText(markdown);
    }

    public string ParseToJson(string markdown)
    {
        return _parser.ParseToJson(markdown);
    }

    public void Dispose()
    {
        _parser?.Dispose();
        GC.SuppressFinalize(this);
    }
}
