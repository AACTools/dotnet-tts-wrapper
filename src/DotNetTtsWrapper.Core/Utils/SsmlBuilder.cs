using System.Text;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Utils;

public class SsmlBuilder
{
    private readonly StringBuilder _sb = new();
    private bool _speakClosed;
    private bool _voiceOpen;
    private bool _prosodyOpen;

    public static SsmlBuilder Create()
    {
        var builder = new SsmlBuilder();
        builder._sb.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">");
        return builder;
    }

    [Obsolete("Use Create() instead")]
    public static SsmlBuilder Speak() => Create();

    public SsmlBuilder Voice(string voiceId)
    {
        _sb.Append($"<voice name=\"{System.Security.SecurityElement.Escape(voiceId)}\">");
        _voiceOpen = true;
        return this;
    }

    public SsmlBuilder BeginProsody(SpeechRate? rate, SpeechPitch? pitch, int? volume)
    {
        var parts = new List<string>();
        if (rate.HasValue)
            parts.Add($"rate=\"{rate.Value.ToString().ToLowerInvariant()}\"");
        if (pitch.HasValue)
            parts.Add($"pitch=\"{pitch.Value.ToString().ToLowerInvariant()}\"");
        if (volume.HasValue)
            parts.Add($"volume=\"{volume.Value}\"");

        if (parts.Count > 0)
        {
            _sb.Append($"<prosody {string.Join(" ", parts)}>");
            _prosodyOpen = true;
        }
        return this;
    }

    public SsmlBuilder EndProsody()
    {
        if (_prosodyOpen)
        {
            _sb.Append("</prosody>");
            _prosodyOpen = false;
        }
        return this;
    }

    public SsmlBuilder EndVoice()
    {
        if (_voiceOpen)
        {
            EndProsody();
            _sb.Append("</voice>");
            _voiceOpen = false;
        }
        return this;
    }

    public SsmlBuilder Break(string time)
    {
        _sb.Append($"<break time=\"{System.Security.SecurityElement.Escape(time)}\"/>");
        return this;
    }

    public SsmlBuilder Emphasis(string level, string text)
    {
        _sb.Append($"<emphasis level=\"{System.Security.SecurityElement.Escape(level)}\">{System.Security.SecurityElement.Escape(text)}</emphasis>");
        return this;
    }

    public SsmlBuilder AddText(string text)
    {
        _sb.Append(System.Security.SecurityElement.Escape(text));
        return this;
    }

    public SsmlBuilder SayAs(string interpretAs, string text)
    {
        _sb.Append($"<say-as interpret-as=\"{System.Security.SecurityElement.Escape(interpretAs)}\">{System.Security.SecurityElement.Escape(text)}</say-as>");
        return this;
    }

    public SsmlBuilder Phoneme(string alphabet, string ph, string text)
    {
        _sb.Append($"<phoneme alphabet=\"{System.Security.SecurityElement.Escape(alphabet)}\" ph=\"{System.Security.SecurityElement.Escape(ph)}\">{System.Security.SecurityElement.Escape(text)}</phoneme>");
        return this;
    }

    public string Build()
    {
        if (!_speakClosed)
        {
            EndProsody();
            EndVoice();
            _sb.Append("</speak>");
            _speakClosed = true;
        }
        return _sb.ToString();
    }
}
