using System.Text;

namespace DotNetTtsWrapper.Utils;

/// <summary>
/// Fluent SSML builder for creating speech synthesis markup
/// </summary>
public class SsmlBuilder
{
    private readonly StringBuilder _sb = new();
    private bool _isSpeakClosed = false;

    /// <summary>
    /// Start a new SSML document
    /// </summary>
    public static SsmlBuilder Speak()
    {
        var builder = new SsmlBuilder();
        builder._sb.AppendLine("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">");
        return builder;
    }

    /// <summary>
    /// Set the voice for synthesis
    /// </summary>
    public SsmlBuilder Voice(string voiceId)
    {
        _sb.AppendLine($"<voice name=\"{voiceId}\">");
        return this;
    }

    /// <summary>
    /// Add prosody (rate, pitch, volume)
    /// </summary>
    public SsmlBuilder WithRate(string rate)
    {
        _sb.AppendLine($"<prosody rate=\"{rate}\">");
        return this;
    }

    /// <summary>
    /// Add prosody (rate, pitch, volume)
    /// </summary>
    public SsmlBuilder WithPitch(string pitch)
    {
        _sb.AppendLine($"<prosody pitch=\"{pitch}\">");
        return this;
    }

    /// <summary>
    /// Add prosody (rate, pitch, volume)
    /// </summary>
    public SsmlBuilder WithVolume(int volume)
    {
        _sb.AppendLine($"<prosody volume=\"{volume}\">");
        return this;
    }

    /// <summary>
    /// Add a break/pause
    /// </summary>
    public SsmlBuilder Break(string time)
    {
        _sb.AppendLine($"<break time=\"{time}\"/>");
        return this;
    }

    /// <summary>
    /// Add emphasis
    /// </summary>
    public SsmlBuilder Emphasis(string level, string text)
    {
        _sb.AppendLine($"<emphasis level=\"{level}\">{text}</emphasis>");
        return this;
    }

    /// <summary>
    /// Add plain text
    /// </summary>
    public SsmlBuilder AddText(string text)
    {
        // Escape XML special characters
        var escaped = System.Security.SecurityElement.Escape(text);
        _sb.AppendLine(escaped);
        return this;
    }

    /// <summary>
    /// Say-as element for specifying how text should be interpreted
    /// </summary>
    public SsmlBuilder SayAs(string interpretAs, string text)
    {
        _sb.AppendLine($"<say-as interpret-as=\"{interpretAs}\">{text}</say-as>");
        return this;
    }

    /// <summary>
    /// Phoneme element for pronunciation
    /// </summary>
    public SsmlBuilder Phoneme(string alphabet, string ph, string text)
    {
        _sb.AppendLine($"<phoneme alphabet=\"{alphabet}\" ph=\"{ph}\">{text}</phoneme>");
        return this;
    }

    /// <summary>
    /// Close the current element
    /// </summary>
    public SsmlBuilder Close()
    {
        _sb.AppendLine("</prosody>");
        return this;
    }

    /// <summary>
    /// Build the final SSML string
    /// </summary>
    public string Build()
    {
        if (!_isSpeakClosed)
        {
            _sb.AppendLine("</speak>");
            _isSpeakClosed = true;
        }
        return _sb.ToString();
    }
}