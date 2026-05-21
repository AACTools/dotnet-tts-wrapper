namespace DotNetTtsWrapper.Events;

/// <summary>
/// Event arguments for TTS playback events
/// </summary>
public class PlaybackEventArgs : EventArgs
{
    /// <summary>
    /// The text being spoken
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Event arguments for speech started event
/// </summary>
public class SpeechStartedEventArgs : PlaybackEventArgs
{
}

/// <summary>
/// Event arguments for speech completed event
/// </summary>
public class SpeechCompletedEventArgs : PlaybackEventArgs
{
    /// <summary>
    /// Whether the speech was cancelled
    /// </summary>
    public bool WasCancelled { get; set; }

    /// <summary>
    /// Error if one occurred
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Event arguments for audio chunk received event (streaming)
/// </summary>
public class AudioChunkEventArgs : EventArgs
{
    /// <summary>
    /// The audio data chunk
    /// </summary>
    public byte[] AudioData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The format of the audio data
    /// </summary>
    public Models.AudioFormat Format { get; set; }

    /// <summary>
    /// Whether this is the final chunk
    /// </summary>
    public bool IsFinal { get; set; }

    /// <summary>
    /// Current position in the stream
    /// </summary>
    public long Position { get; set; }
}