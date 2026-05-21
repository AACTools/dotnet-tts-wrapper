namespace DotNetTtsWrapper.Events;

/// <summary>
/// Event arguments for word boundary/timing events
/// </summary>
public class WordTimingEventArgs : EventArgs
{
    /// <summary>
    /// The word or text segment
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Start time in seconds
    /// </summary>
    public double StartTime { get; set; }

    /// <summary>
    /// End time in seconds
    /// </summary>
    public double EndTime { get; set; }

    /// <summary>
    /// Duration in seconds
    /// </summary>
    public double Duration => EndTime - StartTime;

    /// <summary>
    /// Confidence score (0-1) if available from the engine
    /// </summary>
    public float? Confidence { get; set; }

    /// <summary>
    /// Character-level timing data if available
    /// </summary>
    public List<CharacterTiming>? CharacterTimings { get; set; }

    public WordTimingEventArgs(string text, double startTime, double endTime)
    {
        Text = text;
        StartTime = startTime;
        EndTime = endTime;
    }
}

/// <summary>
/// Character-level timing information
/// </summary>
public class CharacterTiming
{
    /// <summary>
    /// The character
    /// </summary>
    public char Character { get; set; }

    /// <summary>
    /// Start time in seconds
    /// </summary>
    public double StartTime { get; set; }

    /// <summary>
    /// End time in seconds
    /// </summary>
    public double EndTime { get; set; }

    /// <summary>
    /// Duration in seconds
    /// </summary>
    public double Duration => EndTime - StartTime;
}