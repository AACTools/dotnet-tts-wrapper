using System.Speech.Synthesis;
using DotNetTtsWrapper.Events;
using DotNetTtsWrapper.Models;

namespace DotNetTtsWrapper.Engines;

/// <summary>
/// Windows SAPI (Speech API) TTS Client
/// Uses the built-in Windows Speech API via System.Speech
/// </summary>
public class SapiTtsClient : AbstractTtsClient
{
    private readonly SpeechSynthesizer _synthesizer;
    private readonly SemaphoreSlim _synthesisLock = new(1, 1);
    private CancellationTokenSource? _playbackCts;

    public SapiTtsClient()
    {
        // Ensure we're running on Windows
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            throw new PlatformNotSupportedException("SAPI is only supported on Windows");
        }

        _synthesizer = new SpeechSynthesizer();

        // Set up event handlers for word boundaries
        _synthesizer.SpeakStarted += (s, e) =>
        {
            OnSpeechStarted(new DotNetTtsWrapper.Events.SpeechStartedEventArgs());
        };

        _synthesizer.SpeakCompleted += (s, e) =>
        {
            OnSpeechCompleted(new DotNetTtsWrapper.Events.SpeechCompletedEventArgs
            {
                WasCancelled = e.Cancelled,
                Error = e.Error?.Message
            });
        };

        _synthesizer.PhonemeReached += (s, e) =>
        {
            // SAPI provides phoneme-level timing which is more detailed than word boundaries
            // We can aggregate these to create word boundaries
        };

        _synthesizer.VisemeReached += (s, e) =>
        {
            // Viseme events can also be used for timing
        };

        Capabilities = new EngineCapabilities
        {
            SupportsStreaming = false,
            SupportsWordTimings = true,
            SupportsSsml = true,
            SupportsSpeechMarkdown = true,
            RequiresInternet = false,
            IsWindowsSupported = true,
            IsLinuxSupported = false,
            IsMacOsSupported = false
        };

        // Set default voice
        SetDefaultVoice();
    }

    private void SetDefaultVoice()
    {
        try
        {
            var voices = _synthesizer.GetInstalledVoices();
            if (voices.Count > 0)
            {
                var defaultVoice = voices.FirstOrDefault(v => v.Enabled) ?? voices.First();
                VoiceId = defaultVoice.VoiceInfo.Name;
            }
        }
        catch
        {
            // Use system default
        }
    }

    public override async Task<List<TtsVoice>> GetVoicesAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                return _synthesizer.GetInstalledVoices()
                    .Where(v => v.Enabled)
                    .Select(v => new TtsVoice
                    {
                        Id = v.VoiceInfo.Name,
                        Name = v.VoiceInfo.Name,
                        Gender = MapGender(v.VoiceInfo.Gender),
                        Age = MapAge(v.VoiceInfo.Age),
                        Provider = "sapi",
                        LanguageCodes = new List<LanguageInfo>
                        {
                            new LanguageInfo
                            {
                                Bcp47 = v.VoiceInfo.Culture.Name,
                                Iso639_3 = v.VoiceInfo.Culture.TwoLetterISOLanguageName,
                                Display = v.VoiceInfo.Culture.DisplayName
                            }
                        },
                        Description = v.VoiceInfo.Description
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve SAPI voices", ex);
            }
        });
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

    public override void SetVoice(string voiceId)
    {
        base.SetVoice(voiceId);
        _synthesizer.SelectVoice(voiceId);
    }

    public override async Task<TtsSynthesisResult> SynthToBytesAsync(string text, TtsOptions? options = null)
    {
        options ??= new TtsOptions();

        await _synthesisLock.WaitAsync();
        try
        {
            var preparedText = await PrepareTextAsync(text, options);

            // Use MemoryStream to capture audio
            using var stream = new MemoryStream();
            _synthesizer.SetOutputToWaveStream(stream);

            // Setup word timing tracking if enabled
            List<WordTimingEventArgs> wordTimings = new();
            if (options.EnableWordTimings)
            {
                SetupWordTimingTracking(wordTimings, preparedText);
            }

            // Speak
            await Task.Run(() => _synthesizer.Speak(preparedText));

            _synthesizer.SetOutputToNull();

            return new TtsSynthesisResult
            {
                AudioData = stream.ToArray(),
                WordTimings = wordTimings,
                Format = AudioFormat.Wav,
                SampleRate = 24000, // SAPI typically uses 24kHz
                Channels = 1
            };
        }
        finally
        {
            _synthesisLock.Release();
        }
    }

    public override async Task<StreamingTtsResult> SynthToStreamAsync(string text, TtsOptions? options = null)
    {
        options ??= new TtsOptions();

        // SAPI doesn't support true streaming, so we'll implement pseudo-streaming
        // by synthesizing the entire audio and then streaming it in chunks
        var synthesisResult = await SynthToBytesAsync(text, options);

        var streamingResult = new StreamingTtsResult
        {
            Format = synthesisResult.Format,
            SampleRate = synthesisResult.SampleRate,
            Channels = synthesisResult.Channels,
            WordTimings = synthesisResult.WordTimings,
            FinalAudioData = synthesisResult.AudioData
        };

        // Create a streaming async enumerable from the complete audio data
        streamingResult.AudioStream = CreateAudioChunkStream(synthesisResult.AudioData, synthesisResult.Format);

        return streamingResult;
    }

    public override async Task SynthToFileAsync(string text, string outputPath, AudioFormat format = AudioFormat.Wav, TtsOptions? options = null)
    {
        options ??= new TtsOptions();

        await _synthesisLock.WaitAsync();
        try
        {
            var preparedText = await PrepareTextAsync(text, options);

            // Set output to file
            _synthesizer.SetOutputToWaveFile(outputPath);

            // Speak
            await Task.Run(() => _synthesizer.Speak(preparedText));

            _synthesizer.SetOutputToNull();
        }
        finally
        {
            _synthesisLock.Release();
        }
    }

    public override async Task SpeakAsync(string text, TtsOptions? options = null)
    {
        options ??= new TtsOptions();

        await _synthesisLock.WaitAsync();
        try
        {
            var preparedText = await PrepareTextAsync(text, options);
            _playbackCts = new CancellationTokenSource();

            // Setup word timing tracking if enabled
            if (options.EnableWordTimings)
            {
                SetupWordTimingTracking(CurrentWordTimings, preparedText);
            }

            // Speak to default audio output
            await Task.Run(() =>
            {
                _synthesizer.SpeakAsyncCancelAll(); // Cancel any previous speech
                _synthesizer.SpeakAsync(preparedText);
            }, _playbackCts.Token);

            PlaybackState.IsPlaying = true;
        }
        finally
        {
            _synthesisLock.Release();
        }
    }

    public override async Task SpeakStreamedAsync(string text, Action<WordTimingEventArgs>? wordCallback = null, TtsOptions? options = null)
    {
        options ??= new TtsOptions();

        // SAPI doesn't support true streaming, so we'll use SpeakAsync with word callbacks
        if (wordCallback != null)
        {
            WordBoundary += (s, e) => wordCallback(e);
        }

        await SpeakAsync(text, options);
    }

    public override void Pause()
    {
        if (PlaybackState.IsPlaying && !PlaybackState.IsPaused)
        {
            if (_synthesizer != null)
            {
                _synthesizer.Pause();
            }
            PlaybackState.IsPaused = true;
        }
    }

    public override void Resume()
    {
        if (PlaybackState.IsPaused)
        {
            if (_synthesizer != null)
            {
                _synthesizer.Resume();
            }
            PlaybackState.IsPaused = false;
        }
    }

    public override void Stop()
    {
        _playbackCts?.Cancel();
        if (_synthesizer != null)
        {
            try
            {
                _synthesizer.SpeakAsyncCancelAll();
            }
            catch (ObjectDisposedException)
            {
                // Ignore if already disposed
            }
        }
        PlaybackState.IsPlaying = false;
        PlaybackState.IsPaused = false;
    }

    public override async Task<CredentialsValidationResult> CheckCredentialsAsync()
    {
        // SAPI doesn't require credentials, just verify it's working
        try
        {
            var voices = await GetVoicesAsync();
            return new CredentialsValidationResult
            {
                IsValid = voices.Count > 0,
                AvailableVoiceCount = voices.Count,
                EngineName = "sapi"
            };
        }
        catch (Exception ex)
        {
            return new CredentialsValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                EngineName = "sapi"
            };
        }
    }

    private void SetupWordTimingTracking(List<WordTimingEventArgs> wordTimings, string text)
    {
        // SAPI doesn't provide precise word timing, so we'll estimate
        // This is a simplified implementation - real timing would require more complex logic
        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        double currentTime = 0;
        double averageWordDuration = 0.5; // 500ms per word estimate

        foreach (var word in words)
        {
            var wordDuration = CalculateWordDuration(word, averageWordDuration);
            var timing = new WordTimingEventArgs(word, currentTime, currentTime + wordDuration);
            wordTimings.Add(timing);

            // Schedule the word boundary event
            Task.Delay(TimeSpan.FromSeconds(currentTime)).ContinueWith(_ =>
            {
                OnWordBoundary(timing);
            });

            currentTime += wordDuration;
        }
    }

    private double CalculateWordDuration(string word, double baseDuration)
    {
        // Simple heuristic: longer words take longer to pronounce
        return baseDuration * (1 + (word.Length * 0.1));
    }

    private async IAsyncEnumerable<AudioChunkEventArgs> CreateAudioChunkStream(byte[] audioData, AudioFormat format)
    {
        const int chunkSize = 4096; // 4KB chunks
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

            // Small delay to simulate real streaming
            await Task.Delay(50);
        }
    }

    private static DotNetTtsWrapper.Models.VoiceGender MapGender(System.Speech.Synthesis.VoiceGender gender)
    {
        return gender switch
        {
            System.Speech.Synthesis.VoiceGender.Male => DotNetTtsWrapper.Models.VoiceGender.Male,
            System.Speech.Synthesis.VoiceGender.Female => DotNetTtsWrapper.Models.VoiceGender.Female,
            System.Speech.Synthesis.VoiceGender.Neutral => DotNetTtsWrapper.Models.VoiceGender.Unknown,
            _ => DotNetTtsWrapper.Models.VoiceGender.Unknown
        };
    }

    private static DotNetTtsWrapper.Models.VoiceAge MapAge(System.Speech.Synthesis.VoiceAge age)
    {
        return age switch
        {
            System.Speech.Synthesis.VoiceAge.Adult => DotNetTtsWrapper.Models.VoiceAge.Adult,
            System.Speech.Synthesis.VoiceAge.Child => DotNetTtsWrapper.Models.VoiceAge.Child,
            System.Speech.Synthesis.VoiceAge.Senior => DotNetTtsWrapper.Models.VoiceAge.Senior,
            _ => DotNetTtsWrapper.Models.VoiceAge.Unknown
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stop();
            _synthesizer?.Dispose();
            _synthesisLock?.Dispose();
            _playbackCts?.Dispose();
        }
        base.Dispose(disposing);
    }
}