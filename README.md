# DotNet TTS Wrapper

A .NET NuGet package that provides a unified API for working with multiple cloud-based and local Text-to-Speech (TTS) services. Ported from [js-tts-wrapper](https://github.com/AACTools/js-tts-wrapper).

**Repository**: https://github.com/AACTools/dotnet-tts-wrapper  
**NuGet**: `dotnet add package DotNetTtsWrapper`

## Supported Engines

| Engine | Word Events | Streaming | Offline | Notes |
|--------|-------------|-----------|---------|-------|
| **Azure** | Real | Yes | No | Azure Speech SDK (WebSocket). Also has REST client. |
| **Google** | Real (timepoints) | Yes | No | Google Cloud TTS |
| **ElevenLabs** | Real (alignment) | Yes | No | Character-level alignment data |
| **Polly** | Estimated | Yes | No | AWS Polly with full Signature V4 auth |
| **OpenAI** | Estimated | Yes | No | Configurable model (tts-1 / tts-1-hd) |
| **Cartesia** | Estimated | Yes | No | Low-latency TTS |
| **Deepgram** | Estimated | Yes | No | Aura models |
| **Watson** | Estimated | Yes | No | IBM Watson TTS |
| **SherpaOnnx** | Estimated | Yes | **Yes** | Local VITS/Matcha/Kokoro/Piper/MMS models |
| **SAPI** | Estimated | No | N/A | Windows built-in system voices |
| PlayHT, WitAI, Gemini, Hume, xAI, FishAudio, Mistral, Murf, UnrealSpeech, Resemble, UpliftAI, ModelsLab | Estimated | Yes | No | Additional cloud engines |

### Word Timing Support

| Type | Description |
|------|-------------|
| **Real** | Engine provides actual word boundary timestamps from the API |
| **Estimated** | Length-weighted heuristic based on speaking rate (150 WPM default, configurable). Automatically applied as fallback when an engine doesn't provide real timing data. |

All engines return `WordTimings` on `TtsSynthesisResult`. Engines without native support get estimated timings automatically — no configuration needed.

## Features

- **Unified API**: Single interface for 20+ TTS engines via `TtsFactory.CreateClient()`
- **Streaming**: `IAsyncEnumerable<AudioChunkEventArgs>` for real-time audio chunk streaming
- **Word Timings**: Real word boundary events from Azure/Google/ElevenLabs; automatic estimated fallback for all other engines via `WordTimingEstimator`
- **SpeechMarkdown**: Automatic conversion from SpeechMarkdown to SSML/plaintext per engine
- **Credential Validation**: `CheckCredentialsAsync()` on every engine, with synthesis fallback for engines with hardcoded voice lists
- **Cross-platform**: Windows, Linux, macOS (engine-dependent)
- **Modern .NET**: Built for .NET 8.0+ with `RollForward=LatestMajor`

## Installation

```bash
dotnet add package DotNetTtsWrapper
```

## Quick Start

```csharp
using DotNetTtsWrapper.Models;
using DotNetTtsWrapper.Engines;

// Create a client (factory handles all engine types)
var creds = new OpenAICredentials { ApiKey = "sk-...", Model = "tts-1-hd" };
var client = TtsFactory.CreateClient("openai", creds);

// List voices
var voices = await client.GetVoicesAsync();
client.SetVoice("alloy");

// Synthesize to bytes (with word timings)
var result = await client.SynthToBytesAsync("Hello world!");
File.WriteAllBytes("output.mp3", result.AudioData);

// Word timings are always available (real or estimated)
foreach (var t in result.WordTimings)
    Console.WriteLine($"{t.Text}: {t.StartTime:F2}s - {t.EndTime:F2}s");
```

## Engine Configuration

### Azure
```csharp
var creds = new AzureCredentials { SubscriptionKey = "key", Region = "eastus" };
var client = TtsFactory.CreateClient("azure", creds);
```

### OpenAI (configurable model)
```csharp
var creds = new OpenAICredentials { ApiKey = "sk-...", Model = "tts-1-hd" };
// Model defaults to "tts-1", set to "tts-1-hd" for higher quality
// OrganizationId optional: creds.OrganizationId = "org-...";
```

### ElevenLabs (configurable model + voice settings)
```csharp
var creds = new ElevenLabsCredentials {
    ApiKey = "...",
    ModelId = "eleven_multilingual_v2",  // or "eleven_monolingual_v1"
    Stability = 0.5f,
    SimilarityBoost = 0.75f
};
```

### Google
```csharp
var creds = new GoogleCredentials { ApiKey = "AIza..." };
// languageCode is derived from voice name automatically
```

### AWS Polly (full Signature V4 authentication)
```csharp
var creds = new PollyCredentials {
    AccessKeyId = "AKIA...",
    SecretAccessKey = "...",
    Region = "us-east-1"
};
```

### SherpaOnnx (local offline TTS)
```csharp
var creds = new SherpaOnnxCredentials {
    ModelFilePath = "/path/to/model.onnx",       // explicit paths
    TokensFilePath = "/path/to/tokens.txt",
    DataDirPath = "/path/to/espeak-ng-data",
    // OR use ModelPath directory convention:
    // ModelPath = "/path/to/model/directory",
    // ModelId = "vits-piper-en_US-amy-low"
};
```

## Streaming

```csharp
var streamResult = await client.SynthToStreamAsync("Long text to stream...");
await foreach (var chunk in streamResult.AudioStream)
{
    speaker.Write(chunk.AudioData, 0, chunk.AudioData.Length);
}
// streamResult.WordTimings available after completion
```

## Word Boundary Events

```csharp
// Real-time events during SpeakAsync
client.WordBoundary += (sender, e) => {
    Console.WriteLine($"Word: {e.Text}, Time: {e.StartTime:F2}s");
};
await client.SpeakAsync("Hello world!");

// Or access from synthesis result
var result = await client.SynthToBytesAsync("Hello world!");
var timings = result.WordTimings; // always populated (real or estimated)
```

### Customizing Estimates
```csharp
using DotNetTtsWrapper.Utils;

// Length-weighted estimate (default: 150 WPM)
var estimates = WordTimingEstimator.EstimateWordBoundaries(text, wordsPerMinute: 200);

// With known audio duration (scales proportionally)
var estimates = WordTimingEstimator.EstimateWordBoundaries(text, totalDurationSeconds: 5.2);

// Simple flat estimate (300ms per word)
var flat = WordTimingEstimator.EstimateWordBoundariesFlat(text);
```

## SpeechMarkdown

The wrapper automatically converts SpeechMarkdown to engine-appropriate format:

```csharp
// SpeechMarkdown is auto-detected and converted
await client.SpeakAsync("Hello (speed:x-fast)world(/speed)");
```

Each engine gets the correct platform mapping (Azure → Microsoft Azure, Google → Google Assistant, Polly → Amazon Alexa, etc.).

## Requirements

- .NET 8.0+ runtime
- Windows required for SAPI engine; SherpaOnnx works on all platforms
- API keys/credentials for cloud engines

## License

Ported from [js-tts-wrapper](https://github.com/AACTools/js-tts-wrapper) with .NET-specific enhancements.

## Related Projects

- [VoiceGarden-SAPI](https://github.com/AACTools/VoiceGarden-SAPI) — SAPI5 adapter using this library
- [js-tts-wrapper](https://github.com/AACTools/js-tts-wrapper) — JavaScript/TypeScript version
