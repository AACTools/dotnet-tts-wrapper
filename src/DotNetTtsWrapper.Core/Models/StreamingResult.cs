using DotNetTtsWrapper.Events;

namespace DotNetTtsWrapper.Models;

/// <summary>
/// Result of streaming TTS synthesis with word timings
/// </summary>
public class StreamingTtsResult : IAsyncDisposable
{
    /// <summary>
    /// Stream of audio chunks as they are generated
    /// </summary>
    public IAsyncEnumerable<AudioChunkEventArgs>? AudioStream { get; set; }

    /// <summary>
    /// Word timing information (populated as available)
    /// </summary>
    public List<WordTimingEventArgs> WordTimings { get; set; } = new();

    /// <summary>
    /// Final audio data when streaming is complete
    /// </summary>
    public byte[]? FinalAudioData { get; set; }

    /// <summary>
    /// The audio format of the stream
    /// </summary>
    public AudioFormat Format { get; set; } = AudioFormat.Wav;

    /// <summary>
    /// Sample rate in Hz
    /// </summary>
    public int SampleRate { get; set; } = 24000;

    /// <summary>
    /// Number of channels (1 = mono, 2 = stereo)
    /// </summary>
    public int Channels { get; set; } = 1;

    /// <summary>
    /// Bytes per sample (typically 2 for 16-bit audio)
    /// </summary>
    public int BytesPerSample { get; set; } = 2;

    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Cancellation token to stop streaming
    /// </summary>
    public CancellationToken CancellationToken => _cts.Token;

    /// <summary>
    /// Cancel the ongoing streaming operation
    /// </summary>
    public void Cancel()
    {
        _cts.Cancel();
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
    }
}

/// <summary>
/// Result of non-streaming TTS synthesis
/// </summary>
public class TtsSynthesisResult
{
    /// <summary>
    /// The synthesized audio data
    /// </summary>
    public byte[] AudioData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Word timing information (if available and requested)
    /// </summary>
    public List<WordTimingEventArgs> WordTimings { get; set; } = new();

    /// <summary>
    /// The audio format of the result
    /// </summary>
    public AudioFormat Format { get; set; } = AudioFormat.Wav;

    /// <summary>
    /// Sample rate in Hz
    /// </summary>
    public int SampleRate { get; set; } = 24000;

    /// <summary>
    /// Number of channels
    /// </summary>
    public int Channels { get; set; } = 1;
}