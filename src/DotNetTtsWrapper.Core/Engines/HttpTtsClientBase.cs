using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DotNetTtsWrapper.Utils;
using System.Text;
using System.Text.Json;
using DotNetTtsWrapper.Events;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// Base class for HTTP-based TTS clients
/// Provides common functionality for REST API clients
/// </summary>
public abstract class HttpTtsClientBase : AbstractTtsClient
{
    protected readonly HttpClient _httpClient;
    protected readonly SemaphoreSlim _httpLock = new(1, 1);
    protected abstract string BaseEndpoint { get; }
    protected abstract bool SupportsStreaming { get; }

    protected HttpTtsClientBase()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    protected HttpTtsClientBase(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Set authentication header for the HTTP client
    /// </summary>
    protected void SetAuthentication(string scheme, string parameter)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, parameter);
    }

    /// <summary>
    /// Set API key header
    /// </summary>
    protected void SetApiKeyHeader(string headerName, string apiKey)
    {
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(headerName, apiKey);
    }

    /// <summary>
    /// Make HTTP POST request and return response as bytes
    /// </summary>
    protected async Task<byte[]> PostAsBytesAsync(string endpoint, object? payload = null)
    {
        await _httpLock.WaitAsync();
        try
        {
            var fullUrl = $"{BaseEndpoint.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var content = payload != null ? JsonContent.Create(payload) : null;

            var response = await _httpClient.PostAsync(fullUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"TTS request failed: {(int)response.StatusCode} {response.StatusCode}\n{errorBody}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
        finally
        {
            _httpLock.Release();
        }
    }

    /// <summary>
    /// Make HTTP POST request and return response as stream
    /// </summary>
    protected async Task<Stream> PostAsStreamAsync(string endpoint, object? payload = null)
    {
        await _httpLock.WaitAsync();
        try
        {
            var fullUrl = $"{BaseEndpoint.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var content = payload != null ? JsonContent.Create(payload) : null;

            var response = await _httpClient.PostAsync(fullUrl, content);
            if (!response.IsSuccessStatusCode) { var errorBody = await response.Content.ReadAsStringAsync(); throw new HttpRequestException($"TTS request failed: {(int)response.StatusCode} {response.StatusCode}\n{errorBody}"); }

            return await response.Content.ReadAsStreamAsync();
        }
        finally
        {
            _httpLock.Release();
        }
    }

    /// <summary>
    /// Make HTTP POST request and return response as JSON
    /// </summary>
    protected async Task<T> PostAsJsonAsync<T>(string endpoint, object? payload = null)
    {
        await _httpLock.WaitAsync();
        try
        {
            var fullUrl = $"{BaseEndpoint.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var content = payload != null ? JsonContent.Create(payload) : null;

            var response = await _httpClient.PostAsync(fullUrl, content);
            if (!response.IsSuccessStatusCode) { var errorBody = await response.Content.ReadAsStringAsync(); throw new HttpRequestException($"TTS request failed: {(int)response.StatusCode} {response.StatusCode}\n{errorBody}"); }

            return await response.Content.ReadFromJsonAsync<T>() ??
                   throw new InvalidOperationException("Failed to deserialize JSON response");
        }
        finally
        {
            _httpLock.Release();
        }
    }

    /// <summary>
    /// Make HTTP GET request and return response as JSON
    /// </summary>
    protected async Task<T> GetAsJsonAsync<T>(string endpoint)
    {
        await _httpLock.WaitAsync();
        try
        {
            var fullUrl = $"{BaseEndpoint.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var response = await _httpClient.GetAsync(fullUrl);
            if (!response.IsSuccessStatusCode) { var errorBody = await response.Content.ReadAsStringAsync(); throw new HttpRequestException($"TTS request failed: {(int)response.StatusCode} {response.StatusCode}\n{errorBody}"); }

            return await response.Content.ReadFromJsonAsync<T>() ??
                   throw new InvalidOperationException("Failed to deserialize JSON response");
        }
        finally
        {
            _httpLock.Release();
        }
    }

    public override async Task<TtsSynthesisResult> SynthToBytesAsync(string text, TtsOptions? options = null)
    {
        options ??= new TtsOptions();
        var preparedText = await PrepareTextAsync(text, options);

        var payload = await BuildSynthesisPayload(preparedText, options);
        var audioData = await PostAsBytesAsync(GetSynthesisEndpoint(options), payload);

        var wordTimings = CurrentWordTimings;
        if ((wordTimings == null || wordTimings.Count == 0) && options.EnableWordTimings != false)
        {
            var estimated = WordTimingEstimator.EstimateWordBoundaries(preparedText);
            wordTimings = estimated.Select(w => new Events.WordTimingEventArgs(
                w.Word, w.StartSeconds, w.EndSeconds)).ToList();
        }

        return new TtsSynthesisResult
        {
            AudioData = audioData,
            WordTimings = wordTimings ?? new List<WordTimingEventArgs>(),
            Format = options.Format,
            SampleRate = SampleRate
        };
    }

    public override async Task<StreamingTtsResult> SynthToStreamAsync(string text, TtsOptions? options = null)
    {
        options ??= new TtsOptions();
        var preparedText = await PrepareTextAsync(text, options);

        if (!SupportsStreaming)
        {
            // If the engine doesn't support true streaming, fall back to non-streaming
            var synthesisResult = await SynthToBytesAsync(text, options);

            return new StreamingTtsResult
            {
                Format = synthesisResult.Format,
                SampleRate = synthesisResult.SampleRate,
                Channels = synthesisResult.Channels,
                WordTimings = synthesisResult.WordTimings,
                FinalAudioData = synthesisResult.AudioData,
                AudioStream = CreateAsyncEnumerableFromBytes(synthesisResult.AudioData, synthesisResult.Format)
            };
        }

        var payload = await BuildSynthesisPayload(preparedText, options);
        var audioStream = await PostAsStreamAsync(GetStreamingEndpoint(options), payload);

        return new StreamingTtsResult
        {
            AudioStream = CreateAsyncEnumerableFromStream(audioStream, options.Format),
            Format = options.Format,
            SampleRate = SampleRate,
            Channels = 1
        };
    }

    public override async Task SynthToFileAsync(string text, string outputPath, AudioFormat format = AudioFormat.Wav, TtsOptions? options = null)
    {
        options ??= new TtsOptions();
        options.Format = format;

        var audioData = await SynthToBytesAsync(text, options);
        await File.WriteAllBytesAsync(outputPath, audioData.AudioData);
    }

    public override async Task SpeakAsync(string text, TtsOptions? options = null)
    {
        // HTTP clients don't support direct playback
        // We'll synthesize to bytes and then use a simple playback method
        var result = await SynthToBytesAsync(text, options);

        // For now, we'll save to a temp file and use system playback
        // This could be improved with direct audio playback in the future
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFile, result.AudioData);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Use Windows Media Player or similar for playback
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                })?.WaitForExit();
            }
            else
            {
                // For non-Windows, you'd need to implement audio playback differently
                throw new PlatformNotSupportedException("Audio playback not implemented for this platform");
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    public override async Task SpeakStreamedAsync(string text, Action<WordTimingEventArgs>? wordCallback = null, TtsOptions? options = null)
    {
        if (wordCallback != null)
        {
            WordBoundary += (s, e) => wordCallback(e);
        }

        await SpeakAsync(text, options);
    }

    public override void Pause()
    {
        // HTTP clients don't support pause/resume
        throw new NotSupportedException("Pause/resume is not supported for HTTP-based clients");
    }

    public override void Resume()
    {
        // HTTP clients don't support pause/resume
        throw new NotSupportedException("Pause/resume is not supported for HTTP-based clients");
    }

    public override void Stop()
    {
        // For HTTP clients, this would typically cancel ongoing requests
        // Implementation depends on how requests are being handled
    }

    // Abstract methods that specific engines must implement
    protected abstract Task<object> BuildSynthesisPayload(string text, TtsOptions options);
    protected abstract string GetSynthesisEndpoint(TtsOptions options);
    protected abstract string GetStreamingEndpoint(TtsOptions options);
    protected abstract Task<List<TtsVoice>> GetVoicesFromApiAsync();

    public override async Task<List<TtsVoice>> GetVoicesAsync()
    {
        return await GetVoicesFromApiAsync();
    }

    public override async Task<List<TtsVoice>> GetVoicesByLanguageAsync(string languageCode)
    {
        var allVoices = await GetVoicesAsync();
        return allVoices
            .Where(v => v.LanguageCodes.Any(l =>
                l.Bcp47.Equals(languageCode, StringComparison.OrdinalIgnoreCase) ||
                l.Iso639_3.Equals(languageCode, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    protected async IAsyncEnumerable<AudioChunkEventArgs> CreateAsyncEnumerableFromBytes(byte[] audioData, AudioFormat format)
    {
        const int chunkSize = 4096;
        var position = 0;

        while (position < audioData.Length)
        {
            var remainingBytes = audioData.Length - position;
            var chunkSizeToUse = Math.Min(chunkSize, remainingBytes);

            var chunk = new byte[chunkSizeToUse];
            Array.Copy(audioData, position, chunk, 0, chunkSizeToUse);

            yield return new AudioChunkEventArgs
            {
                AudioData = chunk,
                Format = format,
                IsFinal = (position + chunkSizeToUse) >= audioData.Length,
                Position = position
            };

            position += chunkSizeToUse;
            await Task.Delay(10); // Small delay to simulate streaming
        }
    }

    protected async IAsyncEnumerable<AudioChunkEventArgs> CreateAsyncEnumerableFromStream(Stream audioStream, AudioFormat format)
    {
        var buffer = new byte[4096];
        var position = 0;
        var isFinal = false;

        while (!isFinal)
        {
            var bytesRead = await audioStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                isFinal = true;
                continue;
            }

            var chunk = new byte[bytesRead];
            Array.Copy(buffer, chunk, bytesRead);

            yield return new AudioChunkEventArgs
            {
                AudioData = chunk,
                Format = format,
                IsFinal = isFinal,
                Position = position
            };

            position += bytesRead;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
            _httpLock?.Dispose();
        }
        base.Dispose(disposing);
    }
}
