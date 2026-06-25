namespace DotNetTtsWrapper.Utils;

/// <summary>
/// Estimates word boundary timings for engines that don't provide real word timing data.
/// Uses a length-weighted model based on a configurable speaking rate.
/// </summary>
public static class WordTimingEstimator
{
    /// <summary>
    /// Estimate word boundaries based on text length and a configurable speaking rate.
    /// </summary>
    /// <param name="text">The text being synthesized.</param>
    /// <param name="totalDurationSeconds">Optional: if known, scale estimates to fit the actual audio duration.</param>
    /// <param name="wordsPerMinute">Speaking rate (default 150 WPM).</param>
    /// <returns>List of estimated word timings.</returns>
    public static List<WordBoundary> EstimateWordBoundaries(
        string text,
        double? totalDurationSeconds = null,
        int wordsPerMinute = 150)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new();

        // Split into words, preserving character offsets
        var words = new List<(string word, int offset)>();
        int pos = 0;
        foreach (var segment in text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            int idx = text.IndexOf(segment, pos, StringComparison.Ordinal);
            if (idx < 0) idx = pos;
            words.Add((segment, idx));
            pos = idx + segment.Length;
        }

        if (words.Count == 0)
            return new();

        // Calculate base duration per word
        double baseMsPerWord = 60000.0 / wordsPerMinute;

        // Length-weighted duration per word (clamped to [0.5, 2.0] of base)
        var durations = new double[words.Count];
        double totalEstimatedMs = 0;
        for (int i = 0; i < words.Count; i++)
        {
            double lengthFactor = Math.Clamp(words[i].word.Length / 5.0, 0.5, 2.0);
            durations[i] = baseMsPerWord * lengthFactor;
            totalEstimatedMs += durations[i];
        }

        // If actual audio duration is known, scale estimates proportionally
        double scaleFactor = 1.0;
        if (totalDurationSeconds.HasValue && totalEstimatedMs > 0)
        {
            double actualMs = totalDurationSeconds.Value * 1000;
            scaleFactor = actualMs / totalEstimatedMs;
        }

        var result = new List<WordBoundary>();
        double currentMs = 0;
        for (int i = 0; i < words.Count; i++)
        {
            double startMs = currentMs;
            double durationMs = durations[i] * scaleFactor;

            result.Add(new WordBoundary
            {
                Word = words[i].word,
                TextOffset = words[i].offset,
                TextLength = words[i].word.Length,
                StartSeconds = startMs / 1000.0,
                EndSeconds = (startMs + durationMs) / 1000.0
            });

            currentMs += durationMs;
        }

        return result;
    }

    /// <summary>
    /// Quick flat estimate: 300ms per word, no length weighting.
    /// Matches the JS wrapper's simple fallback.
    /// </summary>
    public static List<WordBoundary> EstimateWordBoundariesFlat(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new();

        var words = text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<WordBoundary>();
        double t = 0;
        int pos = 0;

        foreach (var word in words)
        {
            int offset = text.IndexOf(word, pos, StringComparison.Ordinal);
            if (offset < 0) offset = pos;

            result.Add(new WordBoundary
            {
                Word = word,
                TextOffset = offset,
                TextLength = word.Length,
                StartSeconds = t,
                EndSeconds = t + 0.3
            });
            t += 0.3;
            pos = offset + word.Length;
        }

        return result;
    }
}

/// <summary>
/// A single estimated or real word boundary event.
/// </summary>
public class WordBoundary
{
    public string Word { get; set; } = "";
    public int TextOffset { get; set; }
    public int TextLength { get; set; }
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
}
